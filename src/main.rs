#![cfg_attr(all(windows, not(debug_assertions)), windows_subsystem = "windows")]
mod audio;
mod models;
mod network;
use audio::AudioPlayer;
use eframe::egui;
use network::Event;
use std::{path::PathBuf, sync::mpsc, time::Duration};
use tokio::{runtime::Runtime, task::JoinHandle};

#[derive(PartialEq, Eq, Clone, Copy)]
enum DeathAction { Quieten, Stop, Disable }
#[derive(PartialEq, Eq, Clone, Copy)]
enum LeaveAction { Stop, Disable }

struct App {
    runtime: Runtime,
    network: Option<JoinHandle<()>>,
    rx: Option<mpsc::Receiver<Event>>,
    download: Option<JoinHandle<()>>,
    audio_rx: Option<mpsc::Receiver<(String, Result<PathBuf, String>)>>,
    player: Option<AudioPlayer>,
    username: String,
    connected: bool,
    active: bool,
    volume: f32,
    muted: bool,
    now_playing: String,
    death: DeathAction,
    leave: LeaveAction,
    logs: Vec<String>,
}
impl App {
    fn new(runtime: Runtime) -> Self {
        Self { runtime, network: None, rx: None, download: None, audio_rx: None, player: None,
            username: String::new(), connected: false, active: false, volume: 70.0, muted: false,
            now_playing: "-".into(), death: DeathAction::Quieten, leave: LeaveAction::Stop, logs: vec![] }
    }
    fn log(&mut self, text: impl Into<String>) {
        self.logs.push(text.into());
        if self.logs.len() > 500 { self.logs.remove(0); }
    }
    fn gain(&self) -> f32 { if self.muted { 0.0 } else { self.volume / 100.0 } }
    fn sync_volume(&self) { if let Some(p) = &self.player { p.set_volume(self.gain()); } }
    fn connect(&mut self) {
        if self.username.trim().is_empty() { self.log("Enter your Roblox username first."); return; }
        match AudioPlayer::new() {
            Ok(p) => self.player = Some(p),
            Err(e) => { self.log(format!("Audio device error: {e}")); return; }
        }
        let (tx, rx) = mpsc::channel();
        self.rx = Some(rx);
        self.network = Some(self.runtime.spawn(network::run(self.username.trim().to_owned(), tx)));
        self.log("Connecting...");
    }
    fn stop_audio(&mut self) {
        if let Some(task) = self.download.take() { task.abort(); }
        self.audio_rx = None;
        if let Some(p) = &mut self.player { p.stop(); }
        self.now_playing = "-".into();
    }
    fn disconnect(&mut self) {
        if let Some(task) = self.network.take() { task.abort(); }
        self.rx = None;
        self.stop_audio();
        self.player = None;
        self.connected = false;
        self.active = false;
        self.log("Disconnected.");
    }
    fn play_url(&mut self, url: String) {
        self.stop_audio();
        self.active = true;
        self.now_playing = "Loading...".into();
        let (tx, rx) = mpsc::channel();
        self.audio_rx = Some(rx);
        self.download = Some(self.runtime.spawn(async move {
            let result = audio::download(&url).await.map_err(|e| e.to_string());
            let _ = tx.send((url, result));
        }));
    }
    fn handle(&mut self, event: Event) {
        match event {
            Event::Connected => { self.connected = true; self.log("Connected to server."); }
            Event::Log(s) => self.log(s),
            Event::Closed(s) => { self.log(s); self.disconnect(); }
            Event::Message(m) => match (m.msg_type.as_deref(), m.status_type.as_deref()) {
                (Some("bgm"), _) if m.audio_url.is_some() => self.play_url(m.audio_url.unwrap()),
                (Some("gameStatus"), Some("died")) => match self.death {
                    DeathAction::Quieten => { self.volume /= 2.0; self.sync_volume(); self.log("You died: BGM quietened."); }
                    DeathAction::Stop => { self.stop_audio(); self.log("You died: BGM stopped."); }
                    DeathAction::Disable => self.log("Death handling disabled."),
                },
                (Some("gameStatus"), Some("left")) => {
                    if self.leave == LeaveAction::Stop { self.stop_audio(); }
                    self.active = false;
                    self.log("Left the game.");
                }
                _ => self.log(format!("Unhandled message: {:?}", m)),
            }
        }
    }
    fn poll(&mut self) {
        // Bound work per GUI frame; keep the UI responsive during message bursts.
        for _ in 0..128 {
            let event = self.rx.as_ref().and_then(|rx| rx.try_recv().ok());
            match event { Some(e) => self.handle(e), None => break }
        }
        if let Some((url, result)) = self.audio_rx.as_ref().and_then(|rx| rx.try_recv().ok()) {
            self.audio_rx = None;
            self.download = None;
            let gain = self.gain();
            let result = result.and_then(|path| self.player.as_mut().ok_or_else(|| "No audio device".to_owned())?.play(&path, gain));
            match result {
                Ok(()) => { self.now_playing = url.clone(); self.log(format!("Playing {url}")); }
                Err(e) => { self.now_playing = "-".into(); self.log(format!("Audio error: {e}")); }
            }
        }
    }
}
impl eframe::App for App {
    fn update(&mut self, ctx: &egui::Context, _: &mut eframe::Frame) {
        self.poll();
        ctx.request_repaint_after(Duration::from_millis(50));
        egui::CentralPanel::default().show(ctx, |ui| {
            ui.heading("FE2.IO Native");
            ui.label("Enter Username:");
            ui.horizontal(|ui| {
                ui.add_enabled(self.network.is_none(), egui::TextEdit::singleline(&mut self.username).desired_width(240.0));
                let label = if self.network.is_some() { "Disconnect" } else { "Connect" };
                if ui.button(label).clicked() { if self.network.is_some() { self.disconnect(); } else { self.connect(); } }
            });
            let status = if self.active { "Connected - game session active" } else if self.connected { "Connected (waiting for a game to sync to)" } else if self.network.is_some() { "Connecting..." } else { "Disconnected" };
            ui.colored_label(if self.connected { egui::Color32::from_rgb(80, 180, 120) } else { egui::Color32::from_rgb(220, 110, 100) }, status);
            ui.label(format!("Now playing: {}", self.now_playing));
            ui.add_enabled_ui(self.active, |ui| {
                ui.horizontal(|ui| {
                    if ui.add(egui::Slider::new(&mut self.volume, 0.0..=100.0).suffix("%").fixed_decimals(0).text("Volume")).changed() { self.muted = false; self.sync_volume(); }
                    if ui.checkbox(&mut self.muted, "Mute").changed() { self.sync_volume(); }
                });
            });
            ui.group(|ui| {
                ui.label("On Death:");
                ui.horizontal_wrapped(|ui| {
                    ui.radio_value(&mut self.death, DeathAction::Quieten, "Quieten BGM");
                    ui.radio_value(&mut self.death, DeathAction::Stop, "Stop BGM");
                    ui.radio_value(&mut self.death, DeathAction::Disable, "Disable");
                });
            });
            ui.group(|ui| {
                ui.label("Leaving Game:");
                ui.horizontal(|ui| {
                    ui.radio_value(&mut self.leave, LeaveAction::Stop, "Stop BGM");
                    ui.radio_value(&mut self.leave, LeaveAction::Disable, "Disable");
                });
            });
            ui.separator();
            ui.label("Log");
            egui::ScrollArea::vertical().stick_to_bottom(true).auto_shrink([false, false]).show(ui, |ui| {
                for line in &self.logs { ui.monospace(line); }
            });
        });
    }
}
fn main() -> eframe::Result<()> {
    let runtime = Runtime::new().expect("Cannot start Tokio runtime");
    let options = eframe::NativeOptions { viewport: egui::ViewportBuilder::default().with_inner_size([520.0, 500.0]), ..Default::default() };
    eframe::run_native("FE2.IO Native", options, Box::new(move |_cc| Ok(Box::new(App::new(runtime)))))
}
