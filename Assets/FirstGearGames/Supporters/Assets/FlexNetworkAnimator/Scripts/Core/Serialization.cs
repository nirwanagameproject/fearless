#if MIRAGE
using Mirage;
#else
using Mirror;
#endif
using System;
using System.Collections.Generic;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkAnimators
{



    public static class Serialization
    {
        public static PooledNetworkWriter SerializeAnimatorUpdate(List<AnimatorUpdate> datas, int index)
        {
            using (PooledNetworkWriter writer = NetworkWriterPool.GetWriter())
            {
                writer.WriteByte(datas[index].ComponentIndex);
                Compressions.WriteCompressedUInt(writer, datas[index].NetworkIdentity);

                Compressions.WriteCompressedInt(writer, datas[index].Data.Count);
                if (datas[index].Data.Array.Length > 0)
                    writer.WriteBytes(datas[index].Data.Array, 0, datas[index].Data.Count);

                return writer;
            }
        }

        public static void DeserializeAnimatorUpdate(ref AnimatorUpdate au, ref int readPosition, ArraySegment<byte> data)
        {
            using (PooledNetworkReader reader = NetworkReaderPool.GetReader(data))
            {
                reader.Position = readPosition;

                au.ComponentIndex = reader.ReadByte();
                au.NetworkIdentity = Compressions.ReadCompressedUInt(reader);

                int dataLength = Compressions.ReadCompressedInt(reader);
                if (dataLength > 0)
                    au.Data = reader.ReadBytesSegment(dataLength);

                readPosition = reader.Position;
           }
        }



    }


}