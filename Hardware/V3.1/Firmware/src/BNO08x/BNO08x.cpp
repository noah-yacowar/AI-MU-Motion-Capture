#include "BNO08x.h"
#include <DataTypes/Quaternion.h>
#include "configuration/TrackerConfiguration.h"

BNO08x::BNO08x(uint8_t sda, uint8_t scl, uint8_t imuAddr, uint8_t intPin)
  : address(imuAddr), sdaPin(sda), sclPin(scl), intPinNumber(intPin),
    gpioInt(intPin), intPinInterface(&gpioInt) {}

bool BNO08x::begin() 
{
  delay(100);
  Wire.begin(sdaPin, sclPin);
  Wire.setClock(100000); 
  delay(100);

  imu.enableDebugging(Serial);
  if (!imu.begin(address, Wire, intPinInterface)) 
  {
    Serial.println("Failed to initialize BNO080.");
    return false;
  }

  Serial.println("BNO080 connected.");
  imu.enableGameRotationVector(IMU_PERIOD_MS);  // 100Hz

  // Apply a fixed 90° pitch offset
  offsetQuat = Quaternion::EulerToQuat(90.0f, 0.0f, 0.0f);

  return true;
}

bool BNO08x::update() 
{
  if (imu.dataAvailable()) {
    //imu.getGameQuat(qx, qy, qz, qw, accuracy);
    float radAccuracy;
    imu.getQuat(qx, qy, qz, qw, radAccuracy, accuracy);
    //Serial.printf("Quat: %.4f, %.4f, %.4f, %.4f | Accuracy: %d\n",
    //              qx, qy, qz, qw, accuracy);
    return true;
  }
  return false;
}

String BNO08x::getQuatString(const Quaternion& q)
{
    return String(q.x, 4) + "," + String(q.y, 4) + "," + String(q.z, 4) + "," + String(q.w, 4);
}

Quaternion BNO08x::GetCurrentQuaternion() 
{
    return Quaternion(qx, qy, qz, qw);
}

bool BNO08x::Calibrate() 
{
  /* This function is no longer needed */

    Serial.println("Waiting for IMU reading to calibrate...");

    // Wait until we get a valid reading
    while (!update()) 
    {
        delay(10); // small delay to avoid busy loop
    }

    Quaternion identityQuat = GetCurrentQuaternion();
    offsetQuat = identityQuat.inverse();  // store inverse as offset

    Serial.println("Calibration complete.");

    return true;
}

Quaternion BNO08x::GetCorrectedQuaternion() 
{
  return offsetQuat * GetCurrentQuaternion();
}
