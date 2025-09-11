// Quaternion.h
#ifndef QUATERNION_H
#define QUATERNION_H

#include <Arduino.h>
#include "Vector3.h"

class Quaternion 
{
public:
    float w, x, y, z;

    Quaternion();
    Quaternion(float x, float y, float z, float w);

    Quaternion inverse() const;
    Quaternion operator*(const Quaternion& q) const;
    String toString() const;
    static Quaternion FromToRotation(const Vector3& from, const Vector3& to);
    Vector3 Rotate(const Vector3& v) const;
    Quaternion normalized() const;
    static Quaternion fromAxisAngle(const Vector3& axis, float angle);
    static Quaternion EulerToQuat(float rollDeg, float pitchDeg, float yawDeg);

};

#endif
