#pragma once
#include <ESP8266WiFi.h>
#include <WiFiUdp.h>

class ConnectionManager {
public:
    static void begin(const char* ssid, const char* password, const char* serverIp, uint16_t port);
    static void sendMessage(const String& message);

private:
    static WiFiUDP udp;
    static IPAddress serverAddress;
    static uint16_t serverPort;
};
