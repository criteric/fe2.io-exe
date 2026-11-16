use serde::Deserialize;

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ServerMessage {
    pub msg_type: Option<String>,
    pub status_type: Option<String>,
    pub audio_url: Option<String>,
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn protocol() {
        let m: ServerMessage = serde_json::from_str(r#"{"msgType":"bgm","audioUrl":"https://example.com/music.mp3"}"#).unwrap();
        assert_eq!(m.msg_type.as_deref(), Some("bgm"));
        assert!(m.audio_url.is_some());
        let m: ServerMessage = serde_json::from_str(r#"{"msgType":"gameStatus","statusType":"died"}"#).unwrap();
        assert_eq!(m.status_type.as_deref(), Some("died"));
    }
}
