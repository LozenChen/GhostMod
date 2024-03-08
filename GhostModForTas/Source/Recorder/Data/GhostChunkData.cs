using Microsoft.Xna.Framework;
using Monocle;
using System.IO;

namespace Celeste.Mod.GhostModForTas.Recorder.Data;

public struct GhostChunkData {
    public const string ChunkV1 = "data";
    public const string ChunkV2 = "data2";
    public const string ChunkV3 = "data3";
    public const string Chunk = ChunkV3;
    public bool HasPlayer;
    public bool UpdateHair;
    // V1

    public bool InControl;
    public Vector2 Position;
    public Vector2 Speed;
    public float Rotation;
    public Vector2 Scale;
    public Color Color;

    public float SpriteRate;
    public Vector2? SpriteJustify;

    public Facings Facing;

    public string CurrentAnimationID;
    public int CurrentAnimationFrame;

    public Color HairColor;
    public bool HairSimulateMotion;

    // V2

    public Color? DashColor;
    public Vector2 DashDir;
    public bool DashWasB;

    // V3
    public int HairCount;
    public long Time;

    public void Read(BinaryReader reader, int version) {
        UpdateHair = reader.ReadBoolean();
        InControl = reader.ReadBoolean();

        Position = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        Speed = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        Rotation = reader.ReadSingle();
        Scale = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        Color = new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());

        SpriteRate = reader.ReadSingle();
        SpriteJustify = reader.ReadBoolean() ? new Vector2(reader.ReadSingle(), reader.ReadSingle()) : null;

        Facing = (Facings)reader.ReadInt32();

        CurrentAnimationID = reader.ReadNullTerminatedString();
        CurrentAnimationFrame = reader.ReadInt32();

        HairColor = new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        HairSimulateMotion = reader.ReadBoolean();

        if (version < 2) {
            return;
        }

        DashColor = reader.ReadBoolean() ? new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte()) : null;
        DashDir = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        DashWasB = reader.ReadBoolean();

        if (version < 3) {
            return;
        }

        HairCount = reader.ReadInt32();
        Time = reader.ReadInt64();
    }

    public void Write(BinaryWriter writer) {
        writer.Write(UpdateHair);
        writer.Write(InControl);

        writer.Write(Position.X);
        writer.Write(Position.Y);

        writer.Write(Speed.X);
        writer.Write(Speed.Y);

        writer.Write(Rotation);

        writer.Write(Scale.X);
        writer.Write(Scale.Y);

        writer.Write(Color.R);
        writer.Write(Color.G);
        writer.Write(Color.B);
        writer.Write(Color.A);

        writer.Write(SpriteRate);

        if (SpriteJustify != null) {
            writer.Write(true);
            writer.Write(SpriteJustify.Value.X);
            writer.Write(SpriteJustify.Value.Y);
        } else {
            writer.Write(false);
        }

        writer.Write((int)Facing);

        writer.WriteNullTerminatedString(CurrentAnimationID);
        writer.Write(CurrentAnimationFrame);

        writer.Write(HairColor.R);
        writer.Write(HairColor.G);
        writer.Write(HairColor.B);
        writer.Write(HairColor.A);

        writer.Write(HairSimulateMotion);

        if (DashColor == null) {
            writer.Write(false);
        } else {
            writer.Write(true);
            writer.Write(DashColor.Value.R);
            writer.Write(DashColor.Value.G);
            writer.Write(DashColor.Value.B);
            writer.Write(DashColor.Value.A);
        }

        writer.Write(DashDir.X);
        writer.Write(DashDir.Y);

        writer.Write(DashWasB);

        writer.Write(HairCount);
        writer.Write(Time);
    }
}