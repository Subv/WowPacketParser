using System;
using WowPacketParser.Enums;

namespace WowPacketParser.Misc
{
    public sealed class MovementInfo
    {
        public MovementFlag Flags;

        public bool HasSplineData;

        public Vector3 Position;

        public float Orientation;

        public float WalkSpeed;

        public float RunSpeed;

        public UInt32 VehicleId; // Not exactly related to movement but it is read in ReadMovementUpdateBlock
    }
}
