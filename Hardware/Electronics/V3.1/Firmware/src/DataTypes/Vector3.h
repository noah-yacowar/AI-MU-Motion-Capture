#ifndef VECTOR3_H
#define VECTOR3_H

#include <math.h>

struct Vector3 {
    float x, y, z;

    Vector3() : x(0), y(0), z(0) {}
    Vector3(float x_, float y_, float z_) : x(x_), y(y_), z(z_) {}

    float dot(const Vector3& other) const {
        return x * other.x + y * other.y + z * other.z;
    }

    Vector3 cross(const Vector3& other) const {
        return Vector3(
            y * other.z - z * other.y,
            z * other.x - x * other.z,
            x * other.y - y * other.x
        );
    }

    float norm() const {
        return sqrt(x * x + y * y + z * z);
    }

    Vector3 normalized() const {
        float n = norm();
        return (n > 0) ? Vector3(x / n, y / n, z / n) : Vector3(0, 0, 0);
    }

    float angleTo(const Vector3& other) const {
        float dotProd = dot(other.normalized());
        return acosf(fmaxf(-1.0f, fminf(dotProd / (norm()), 1.0f))); // clamp
    }
};

#endif
