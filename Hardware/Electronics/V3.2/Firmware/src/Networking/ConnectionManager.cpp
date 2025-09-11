#include "ConnectionManager.h"

WiFiUDP ConnectionManager::udp;
IPAddress ConnectionManager::serverAddress;
uint16_t ConnectionManager::serverPort = 0;

void ConnectionManager::begin(const char* ssid, const char* password, const char* serverIp, uint16_t port) {
    WiFi.begin(ssid, password);
    while (WiFi.status() != WL_CONNECTED) {
        delay(500);
        Serial.print(".");
    }
    Serial.println("\nWiFi connected!");

    serverAddress.fromString(serverIp);
    serverPort = port;
}

void ConnectionManager::sendMessage(const String& message) {
    udp.beginPacket(serverAddress, serverPort);
    udp.print(message);
    udp.endPacket();
}
