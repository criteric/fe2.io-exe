use crate::models::ServerMessage;
use futures_util::{SinkExt, StreamExt};
use std::sync::mpsc::Sender;
use tokio_tungstenite::{connect_async, tungstenite::Message};

pub enum Event {
    Connected,
    Message(ServerMessage),
    Log(String),
    Closed(String),
}

pub async fn run(username: String, tx: Sender<Event>) {
    let result = session(username, &tx).await;
    let _ = tx.send(Event::Closed(match result {
        Ok(()) => "Connection closed".into(),
        Err(e) => format!("Connection error: {e}"),
    }));
}

async fn session(username: String, tx: &Sender<Event>) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    let (mut socket, _) = tokio::time::timeout(std::time::Duration::from_secs(20), connect_async("ws://client.fe2.io:8081")).await??;
    socket.send(Message::Text(username.into())).await?;
    if tx.send(Event::Connected).is_err() { return Ok(()); }
    while let Some(frame) = socket.next().await {
        match frame? {
            Message::Text(text) => match serde_json::from_str::<ServerMessage>(text.as_ref()) {
                Ok(m) => { if tx.send(Event::Message(m)).is_err() { break; } }
                Err(e) => { let _ = tx.send(Event::Log(format!("Invalid JSON: {e}"))); }
            },
            Message::Ping(data) => { socket.send(Message::Pong(data)).await?; }
            Message::Close(_) => { let _ = socket.flush().await; break; }
            _ => {}
        }
    }
    Ok(())
}
