import { useEffect, useState, useRef } from "react";
import * as signalR from "@microsoft/signalr";

export function useOrderHub() {
  const [orderStatus, setOrderStatus] = useState(null);
  const connectionRef = useRef(null);

  useEffect(() => {
    let isMounted = true;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5000/orderhub", {
        accessTokenFactory: () => localStorage.getItem("token") || "",
      })
      .withAutomaticReconnect()
      .build();

    connectionRef.current = connection;

    // Saga event'lerinden tetiklenen canlı durum dinleyicisi
    connection.on("ReceiveOrderStatus", (status) => {
      if (isMounted) {
        setOrderStatus(status);
      }
    });

    const startConnection = async () => {
      try {
        await connection.start();
        if (isMounted) {
          console.log("SignalR OrderHub bağlantısı kuruldu.");
        }
      } catch (err) {
        // StrictMode veya sayfa geçişlerindeki iptal hatalarını konsola basmıyoruz
        const isAbort =
          err.name === "AbortError" ||
          err.message?.includes("stopped during negotiation");

        if (!isAbort) {
          console.error("SignalR bağlantı hatası:", err);
        }
      }
    };

    startConnection();

    return () => {
      isMounted = false;
      if (connection) {
        connection.off("ReceiveOrderStatus");
        // Sadece bağlıysa veya bağlanıyorsa durdur
        if (
          connection.state === signalR.HubConnectionState.Connected ||
          connection.state === signalR.HubConnectionState.Connecting
        ) {
          connection.stop().catch(() => {});
        }
      }
    };
  }, []);

  return { orderStatus };
}