import { useEffect, useState, useRef } from "react";
import * as signalR from "@microsoft/signalr";

export function useOrderHub() {
  const [orderStatus, setOrderStatus] = useState(null);
  const connectionRef = useRef(null);

  useEffect(() => {
    let isMounted = true;

    // SignalR'ın kendi iç loglayıcısını özelleştiriyoruz
    const customLogger = {
      log: (logLevel, message) => {
        // StrictMode abort logunu konsola basma
        if (message.includes("stopped during negotiation")) return;

        if (logLevel >= signalR.LogLevel.Error) {
          console.error(message);
        } else if (logLevel === signalR.LogLevel.Information && message.includes("WebSocket connected")) {
          console.log(message);
        }
      },
    };

    const connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5000/orderhub", {
        accessTokenFactory: () => localStorage.getItem("token") || "",
      })
      .configureLogging(customLogger) // 👈 Sahte abort logunu filtreler
      .withAutomaticReconnect()
      .build();

    connectionRef.current = connection;

    connection.on("ReceiveOrderStatus", (status) => {
      if (isMounted) {
        setOrderStatus(status);
      }
    });

    connection
      .start()
      .then(() => {
        if (isMounted) {
          console.log("🟢 SignalR OrderHub canlı dinlemede.");
        }
      })
      .catch(() => {
        // StrictMode temizleme hatasını yutuyoruz
      });

    return () => {
      isMounted = false;
      if (connectionRef.current) {
        connectionRef.current.off("ReceiveOrderStatus");
        connectionRef.current.stop().catch(() => {});
      }
    };
  }, []);

  return { orderStatus };
}