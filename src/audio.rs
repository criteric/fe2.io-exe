use rodio::{Decoder, OutputStream, OutputStreamHandle, Sink};
use sha2::{Digest, Sha256};
use std::{fs::File, io::BufReader, path::{Path, PathBuf}};

pub struct AudioPlayer {
    sink: Option<Sink>,
    handle: OutputStreamHandle,
    _stream: OutputStream,
}
impl AudioPlayer {
    pub fn new() -> Result<Self, String> {
        let (stream, handle) = OutputStream::try_default().map_err(|e| e.to_string())?;
        Ok(Self { sink: None, handle, _stream: stream })
    }
    pub fn play(&mut self, path: &Path, volume: f32) -> Result<(), String> {
        self.stop();
        let file = File::open(path).map_err(|e| e.to_string())?;
        let source = Decoder::new(BufReader::new(file)).map_err(|e| e.to_string())?;
        let sink = Sink::try_new(&self.handle).map_err(|e| e.to_string())?;
        sink.set_volume(volume);
        sink.append(source);
        self.sink = Some(sink);
        Ok(())
    }
    pub fn set_volume(&self, volume: f32) { if let Some(sink) = &self.sink { sink.set_volume(volume); } }
    pub fn stop(&mut self) { if let Some(sink) = self.sink.take() { sink.stop(); } }
}

pub async fn download(url: &str) -> Result<PathBuf, Box<dyn std::error::Error + Send + Sync>> {
    let parsed = reqwest::Url::parse(url)?;
    if !matches!(parsed.scheme(), "http" | "https") { return Err("Only HTTP(S) audio is supported".into()); }
    let dir = dirs::home_dir().ok_or("Home directory not found")?.join("fe2io-cache");
    tokio::fs::create_dir_all(&dir).await?;
    let path = dir.join(format!("{:x}.audio", Sha256::digest(url.as_bytes())));
    if tokio::fs::try_exists(&path).await? { return Ok(path); }
    let client = reqwest::Client::builder().timeout(std::time::Duration::from_secs(90)).build()?;
    let mut response = client.get(parsed).send().await?.error_for_status()?;
    let mut bytes = Vec::new();
    while let Some(chunk) = response.chunk().await? {
        if bytes.len() + chunk.len() > 100 * 1024 * 1024 { return Err("Audio exceeds 100 MiB".into()); }
        bytes.extend_from_slice(&chunk);
    }
    let tmp = path.with_extension(format!("{}.tmp", std::process::id()));
    tokio::fs::write(&tmp, bytes).await?;
    tokio::fs::rename(&tmp, &path).await?;
    Ok(path)
}
