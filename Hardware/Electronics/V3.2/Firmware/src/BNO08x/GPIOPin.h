#include <Arduino.h>
#include <string>
#include "PinInterface.h"

class GPIOPin : public PinInterface {
public:
    explicit GPIOPin(int pin) : pinNumber(pin) {}

    bool init() override {
    ::pinMode(pinNumber, INPUT);  // Calls Arduino's pinMode
    return true;
}

    int digitalRead() override {
        return ::digitalRead(pinNumber);
    }

    void pinMode(uint8_t mode) override {
        ::pinMode(pinNumber, mode);
    }

    void digitalWrite(uint8_t val) override {
        ::digitalWrite(pinNumber, val);
    }

    std::string toString() const override {
        return "GPIO " + std::to_string(pinNumber);
    }

private:
    int pinNumber;
};
