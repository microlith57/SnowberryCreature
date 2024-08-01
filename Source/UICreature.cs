using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AsmResolver;
using Microsoft.Xna.Framework;
using Monocle;
using Snowberry;
using Snowberry.UI;

namespace Celeste.Mod.SnowberryCreature;

public class UICreature : UIElement
{
    public static readonly MTexture body, legs, eyes, asleep;

    static UICreature()
    {
        MTexture full = GFX.Gui["SnowberryCreature/the_creature"];

        body = full.GetSubtexture(0, 0, 128, 16);
        legs = full.GetSubtexture(0, 16, 128, 48);
        eyes = full.GetSubtexture(0, 64, 128, 16);
        asleep = full.GetSubtexture(0, 96, 16, 16);
    }

    public class Snore(Vector2 pos)
    {
        public Vector2 Position = pos;
        public float fade = 0f;

        public bool Update(float dt)
        {
            Position += new Vector2(8f, -14f) * dt;
            fade += dt;
            return fade < 1f;
        }

        public void Render()
        {
            Snowberry.Fonts.Pico8.Draw(
                fade < 0.5 ? "Z" : "z",
                Calc.Floor(Position),
                Vector2.One,
                Color.Black * (1 - fade)
            );
        }
    }

    public enum CreatureState
    {
        Walking,
        Idle,
        Sleepy
    }

    public Vector2 Center => Bounds.Center.ToVector2();

    public CreatureState State;
    public Vector2? Target;
    public Vector2 WalkVector => Target.HasValue ? (Target.Value - Center) : Vector2.Zero;
    public float WalkDistance => WalkVector.Length();
    public Vector2 Facing = Vector2.One;

    public const float BACK_AWAY_DIST = 24f;
    public const float TARGET_DIST_LO = 32f;
    public const float TARGET_DIST_HI = 50f;
    public const float WALK_TOWARDS_DIST = 64f;

    public float Sleepiness;
    public float WalkCycle;
    public float BlinkCycle;
    public float SnoreCooldown = 0.4f;
    public List<Snore> Snores = [];

    public UICreature()
    {
        Position = new Vector2(32f, 32f);
        Width = 16;
        Height = 16;
    }

    public override void Update(Vector2 position = default)
    {
        base.Update(position);
        if (!Active)
            return;

        if (Mouse.InBounds && Mouse.IsFocused)
            Target = Mouse.Screen;
        else
            Target = null;

        #region statemachine

        if (Target.HasValue)
        {
            if (State != CreatureState.Walking && (WalkDistance < BACK_AWAY_DIST ||
                                                   WalkDistance > WALK_TOWARDS_DIST))
                State = CreatureState.Walking;
            else if (State == CreatureState.Walking &&
                     WalkDistance > TARGET_DIST_LO &&
                     WalkDistance < TARGET_DIST_HI)
                State = CreatureState.Idle;

            Facing = WalkVector.SafeNormalize();
            if (Facing == Vector2.Zero)
                Facing = Vector2.One;
        } else if (State == CreatureState.Walking) {
            State = CreatureState.Idle;
        }

        if (State == CreatureState.Walking)
            Sleepiness = 0f;
        else if (State == CreatureState.Idle)
            Sleepiness += Engine.RawDeltaTime;

        if (State == CreatureState.Idle && Sleepiness > 5f)
            State = CreatureState.Sleepy;

        #endregion
        #region movement

        if (State == CreatureState.Walking && Target.HasValue)
        {
            bool backingAway = WalkDistance < TARGET_DIST_LO;

            Vector2 real_target = Target.Value - Facing * (backingAway ? 64f : 16f);
            float speed = backingAway ? 16f : Calc.ClampedMap(WalkDistance, 64f, 128f, 64f, 72f);

            Position = Calc.Approach(Position, real_target + (Position - Center), speed * Engine.RawDeltaTime);
        }

        #endregion
        #region animation

        if (State == CreatureState.Walking)
            WalkCycle = (WalkCycle + (Engine.RawDeltaTime / 0.3f)) % 2f;
        else
            WalkCycle = 0f;

        if (State != CreatureState.Sleepy)
            BlinkCycle = (BlinkCycle + (Engine.RawDeltaTime / 0.6f)) % 15f;
        else
            BlinkCycle = 0f;

        if (State == CreatureState.Sleepy)
            if (SnoreCooldown > 0f)
                SnoreCooldown -= Engine.RawDeltaTime;
            else
            {
                if (Snores.Count < 10)
                    Snores.Add(new(new Vector2(Bounds.Right, Bounds.Top)));
                SnoreCooldown = Calc.Random.Range(0.2f, 1.1f);
            }
        else
            SnoreCooldown = 0.4f;

        Snores.RemoveAll((snore) => !snore.Update(Engine.RawDeltaTime));

        #endregion
    }

    public override void Render(Vector2 position = default)
    {
        base.Render(position);

        foreach (var snore in Snores)
            snore.Render();

        if (State == CreatureState.Sleepy)
        {
            asleep.DrawCentered(Center, Color.White, 2f);
            return;
        }

        int dir = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < 8; i++)
        {
            float dist = Math.Abs(Calc.AngleDiff(-(float)Math.PI * 2f * i / 8f, Facing.Angle()));
            if (dist < minDist)
            {
                minDist = dist;
                dir = i;
            }
        }

        body.GetSubtexture(16 * dir, 0, 16, 16).DrawCentered(Center, Color.White, 2f);

        int walkFrame = 0;
        if (State == CreatureState.Walking)
            walkFrame = (int)Math.Floor(WalkCycle) + 1;

        legs.GetSubtexture(16 * dir, 16 * walkFrame, 16, 16).DrawCentered(Center, Color.White, 2f);

        int blinkFrame = (int)Math.Floor(BlinkCycle);
        // if (blinkFrame != 12 && blinkFrame != 14)
        eyes.GetSubtexture(16 * dir, 0, 16, 16).DrawCentered(Center, Color.White, 2f);
    }

}