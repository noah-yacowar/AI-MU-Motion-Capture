// Quaternion.cpp
#include "Quaternion.h"

Quaternion::Quaternion() : w(1), x(0), y(0), z(0) {}

Quaternion::Quaternion(float x_, float y_, float z_, float w_)
    : w(w_), x(x_), y(y_), z(z_) {}

Quaternion Quaternion::EulerToQuat(float rollDeg, float pitchDeg, float yawDeg) {
    float cr = cosf(rollDeg  * 0.5f * DEG_TO_RAD);
    float sr = sinf(rollDeg  * 0.5f * DEG_TO_RAD);
    float cp = cosf(pitchDeg * 0.5f * DEG_TO_RAD);
    float sp = sinf(pitchDeg * 0.5f * DEG_TO_RAD);
    float cy = cosf(yawDeg   * 0.5f * DEG_TO_RAD);
    float sy = sinf(yawDeg   * 0.5f * DEG_TO_RAD);

    Quaternion q;
    q.w = cr * cp * cy + sr * sp * sy;
    q.x = sr * cp * cy - cr * sp * sy;
    q.y = cr * sp * cy + sr * cp * sy;
    q.z = cr * cp * sy - sr * sp * cy;
    return q;
}

Quaternion Quaternion::inverse() const {
    // Assumes unit quaternion
    return Quaternion(-x, -y, -z, w);
}

Quaternion Quaternion::operator*(const Quaternion& q) const {
    return Quaternion(
        w * q.x + x * q.w + y * q.z - z * q.y,
        w * q.y - x * q.z + y * q.w + z * q.x,
        w * q.z + x * q.y - y * q.x + z * q.w,
        w * q.w - x * q.x - y * q.y - z * q.z
    );
}

String Quaternion::toString() const {
    return String(x, 4) + "," + String(y, 4) + "," + String(z, 4) + "," + String(w, 4);
}

Quaternion Quaternion::normalized() const {
    float n = sqrt(x * x + y * y + z * z + w * w);
    return (n > 0) ? Quaternion(x / n, y / n, z / n, w / n) : Quaternion();
}

Quaternion Quaternion::fromAxisAngle(const Vector3& axis, float angle) {
    Vector3 normAxis = axis.normalized();
    float halfAngle = angle * 0.5f;
    float sinHalf = sinf(halfAngle);
    return Quaternion(
        normAxis.x * sinHalf,
        normAxis.y * sinHalf,
        normAxis.z * sinHalf,
        cosf(halfAngle)
    );
}

Vector3 Quaternion::Rotate(const Vector3& v) const
{
    Quaternion vq(v.x, v.y, v.z, 0);
    Quaternion result = (*this) * vq * this->inverse();
    return Vector3(result.x, result.y, result.z);
}

Quaternion Quaternion::FromToRotation(const Vector3& from, const Vector3& to)
{
    Vector3 f = from.normalized();
    Vector3 t = to.normalized();

    float cosTheta = f.dot(t);
    Vector3 rotationAxis;

    if (cosTheta < -1 + 1e-6) {
        // 180 degree rotation around any orthogonal vector
        rotationAxis = Vector3(1, 0, 0).cross(f);
        if (rotationAxis.norm() < 1e-6)
            rotationAxis = Vector3(0, 1, 0).cross(f);
        rotationAxis = rotationAxis.normalized();
        return Quaternion::fromAxisAngle(rotationAxis, M_PI);
    }

    rotationAxis = f.cross(t);
    float s = sqrt((1 + cosTheta) * 2);
    float invs = 1 / s;

    return Quaternion(
        rotationAxis.x * invs,
        rotationAxis.y * invs,
        rotationAxis.z * invs,
        0.5f * s
    ).normalized();
}
