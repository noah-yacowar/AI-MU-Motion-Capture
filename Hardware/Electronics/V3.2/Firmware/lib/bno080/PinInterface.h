#pragma once

#include <cstdint>
#include <string>

class PinInterface
{
public:
	virtual bool init() { return true; };
	virtual int digitalRead() = 0;
	virtual void pinMode(uint8_t mode) = 0;
	virtual void digitalWrite(uint8_t val) = 0;

	[[nodiscard]] virtual std::string toString() const = 0;
};
