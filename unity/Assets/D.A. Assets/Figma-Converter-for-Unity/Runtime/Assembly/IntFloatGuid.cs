using System;

namespace DA_Assets.FCU
{
    public readonly struct IntFloatGuid
    {
        private readonly Guid _guid;
        private IntFloatGuid(Guid g) => _guid = g;
        public Guid Value => _guid;

        public static IntFloatGuid Encode(int hash, float scale)
        {
            var bytes = new byte[16];

            var h = BitConverter.GetBytes(hash);
            var s = BitConverter.GetBytes(scale);

            Buffer.BlockCopy(h, 0, bytes, 0, 4);
            Buffer.BlockCopy(s, 0, bytes, 4, 4);

            return new IntFloatGuid(new Guid(bytes));
        }

        public static (int hash, float scale) Decode(Guid g)
        {
            byte[] b = g.ToByteArray();
            return (BitConverter.ToInt32(b, 0), BitConverter.ToSingle(b, 4));
        }
    }
}