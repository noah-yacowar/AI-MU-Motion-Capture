#pragma once

#include <Wire.h>
#include <BNO080.h>
#include "GPIOPin.h"
#include "DataTypes/Quaternion.h"
#include "DataTypes/Vector3.h"

class BNO08x {
public:
  BNO08x(uint8_t sda, uint8_t scl, uint8_t imuAddr, uint8_t intPin);
  bool begin();
  bool update();
  String getQuatString(const Quaternion& q);
  Quaternion GetCurrentQuaternion();
  bool Calibrate();
  Quaternion GetCorrectedQuaternion();
  
private:
  BNO080 imu;

  uint8_t address;
  uint8_t sdaPin, sclPin, intPinNumber;
  GPIOPin gpioInt;
  PinInterface* intPinInterface;

  const int EEPROM_CALIBRATION_FLAG_ADDR = 0;  // Address for calibration flag in EEPROM (1 byte)
  const int EEPROM_OFFSET_QUATERNION_ADDR = 1;  // Starting address for quaternion offset in EEPROM (16 bytes)

  float qx, qy, qz, qw;
  uint8_t accuracy;

  bool calibrated = false;
  Quaternion offsetQuat;
};
