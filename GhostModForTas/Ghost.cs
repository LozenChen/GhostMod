using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GhostModForTas;

public class Ghost : Actor {
    public GhostManager Manager;

    public Player Player;

    public PlayerSprite Sprite;
    public PlayerHair Hair;
    public int MachineState;

    public GhostData Data;
    public int FrameIndex = 0;
    public GhostFrame? ForcedFrame;
    public GhostFrame PrevFrame => ForcedFrame ?? (Data == null ? default(GhostFrame) : Data[FrameIndex - 1]);
    public GhostFrame Frame => ForcedFrame ?? (Data == null ? default(GhostFrame) : Data[FrameIndex]);
    public bool AutoForward = true;

    public Color Color = Color.White;

    protected float alpha;
    protected float alphaHair;

    public Ghost(Player player)
        : this(player, null) { }

    public Ghost(Player player, GhostData data)
        : base(player.Position) {
        Player = player;
        Data = data;

        Depth = 1;
        // Tag = Tags.PauseUpdate;

        PlayerSpriteMode playerSpriteMode = player.Sprite.Mode;
        if (GhostModule.ModuleSettings.ReversedPlayerSpriteMode) {
            if (playerSpriteMode == PlayerSpriteMode.MadelineAsBadeline) {
                if (player.Inventory.Backpack) {
                    playerSpriteMode = PlayerSpriteMode.MadelineNoBackpack;
                } else {
                    playerSpriteMode = PlayerSpriteMode.Madeline;
                }
            } else {
                playerSpriteMode = PlayerSpriteMode.MadelineAsBadeline;
            }
        }
        Sprite = new PlayerSprite(playerSpriteMode);
        Sprite.HairCount = player.Sprite.HairCount;
        Add(Hair = new PlayerHair(Sprite));
        Add(Sprite);

        Hair.Color = Player.NormalHairColor;
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        Hair.Facing = Frame.Data.Facing;
        Hair.Start();
        UpdateHair();
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
    }

    public void UpdateHair() {
        if (!Frame.Data.IsValid) {
            return;
        }

        Hair.Color = new Color(
            (Frame.Data.HairColor.R * Color.R) / 255,
            (Frame.Data.HairColor.G * Color.G) / 255,
            (Frame.Data.HairColor.B * Color.B) / 255,
            (Frame.Data.HairColor.A * Color.A) / 255
        );
        if (GhostModule.ModuleSettings.ReversedPlayerSpriteMode) {
            if (Hair.Color == Player.NormalHairColor) {
                Hair.Color = Player.NormalBadelineHairColor;
            } else if (Hair.Color == Player.NormalBadelineHairColor) {
                Hair.Color = Player.NormalHairColor;
            }
        }
        Hair.Alpha = alphaHair;
        Hair.Facing = Frame.Data.Facing;
        Hair.SimulateMotion = Frame.Data.HairSimulateMotion;
    }

    public void UpdateSprite() {
        if (!Frame.Data.IsValid) {
            return;
        }

        Position = Frame.Data.Position;
        Sprite.Rotation = Frame.Data.Rotation;
        Sprite.Scale = Frame.Data.Scale;
        Sprite.Scale.X = Sprite.Scale.X * (float) Frame.Data.Facing;
        Sprite.Color = new Color(
            (Frame.Data.Color.R * Color.R) / 255,
            (Frame.Data.Color.G * Color.G) / 255,
            (Frame.Data.Color.B * Color.B) / 255,
            (Frame.Data.Color.A * Color.A) / 255
        ) * alpha;

        Sprite.Rate = Frame.Data.SpriteRate;
        Sprite.Justify = Frame.Data.SpriteJustify;
        Sprite.HairCount = Frame.Data.HairCount;

        try {
            if (Sprite.CurrentAnimationID != Frame.Data.CurrentAnimationID) {
                Sprite.Play(Frame.Data.CurrentAnimationID);
            }

            Sprite.SetAnimationFrame(Frame.Data.CurrentAnimationFrame);
        } catch {
            // Play likes to fail randomly as the ID doesn't exist in an underlying dict.
            // Let's ignore this for now.
        }
    }

    public override void Update() {
        Visible = ForcedFrame != null || ((GhostModule.ModuleSettings.Mode & GhostModuleMode.Play) == GhostModuleMode.Play);
        Visible &= Frame.Data.IsValid;
        if (ForcedFrame == null && Data != null && Data.Dead) {
            Visible &= GhostModule.ModuleSettings.ShowDeaths;
        }

        if (ForcedFrame == null && Data != null && AutoForward) {
            do {
                FrameIndex++;
            } while (
                !PrevFrame.Data.IsValid && FrameIndex < Data.Frames.Count // Skip any frames not containing the data chunk.
            );
        }

        if (Data != null && Data.Opacity != null) {
            alpha = Data.Opacity.Value;
            alphaHair = Data.Opacity.Value;
        } else {
            float dist = (Player.Position - Position).LengthSquared();
            dist -= GhostModule.ModuleSettings.InnerRadiusDist;
            if (dist < 0f) {
                dist = 0f;
            }

            if (GhostModule.ModuleSettings.BorderSize == 0) {
                dist = dist < GhostModule.ModuleSettings.InnerRadiusDist ? 0f : 1f;
            } else {
                dist /= GhostModule.ModuleSettings.BorderSizeDist;
            }

            alpha = Calc.LerpClamp(GhostModule.ModuleSettings.InnerOpacityFactor, GhostModule.ModuleSettings.OuterOpacityFactor, dist);
            alphaHair = Calc.LerpClamp(GhostModule.ModuleSettings.InnerHairOpacityFactor, GhostModule.ModuleSettings.OuterHairOpacityFactor, dist);
        }

        UpdateSprite();
        UpdateHair();

        Visible &= alpha > 0f;

        base.Update();
    }
}