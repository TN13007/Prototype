using Godot;

/// <summary>
/// Visual-only drawing for the player. Reads player state through
/// public getters so gameplay logic stays in Player.cs.
/// </summary>
public partial class PlayerVfx : Node2D
{
    private Player player;

    public override void _Ready()
    {
        ZIndex = 1;
    }


    public void Bind(
        Player owner)
    {
        player = owner;
    }


    public override void _Process(double delta)
    {
        if (player == null ||
            !IsInstanceValid(player))
            return;

        QueueRedraw();
    }


    public override void _Draw()
    {
        if (player == null ||
            !IsInstanceValid(player))
            return;

        DrawAim();

        if (player.IsAttacking)
            DrawSlash();

        if (player.IsDodging)
            DrawDodge();

        if (player.HitVfxTimer > 0f)
            DrawHitVfx();

        DrawStateFlash();
    }


    private void DrawAim()
    {
        DrawLine(
            Vector2.Zero,
            player.AimDirection * 42f,
            new Color(1f, 1f, 1f, 0.18f),
            2f
        );
    }


    private void DrawSlash()
    {
        float duration =
            player.AttackDuration;

        float progress =
            Mathf.Clamp(
                player.AttackTimer / duration,
                0f,
                1f
            );

        float angle =
            player.AimDirection.Angle();

        /*
         * Slash sweeps slightly while the attack happens.
         * The hitbox itself remains tied to the aim direction.
         */
        float sweep =
            Mathf.DegToRad(75f);

        float sweepProgress =
            Mathf.Sin(progress * Mathf.Pi);

        float startAngle =
            angle
            - sweep * 0.5f
            - sweep * 0.25f * sweepProgress;

        float endAngle =
            angle
            + sweep * 0.5f
            + sweep * 0.25f * sweepProgress;

        float radius =
            Mathf.Lerp(
                player.SlashRadius * 0.75f,
                player.SlashRadius,
                progress
            );

        float alpha =
            1f - progress * 0.7f;

        DrawArc(
            Vector2.Zero,
            radius,
            startAngle,
            endAngle,
            20,
            new Color(
                1f,
                1f,
                1f,
                alpha
            ),
            7f,
            true
        );

        DrawArc(
            Vector2.Zero,
            radius + 4f,
            startAngle,
            endAngle,
            20,
            new Color(
                0.7f,
                0.9f,
                1f,
                alpha * 0.6f
            ),
            3f,
            true
        );

        if (player.SlashFlashTimer > 0f)
        {
            Vector2 slashTip =
                player.AimDirection * radius;

            DrawCircle(
                slashTip,
                5f,
                new Color(
                    1f,
                    1f,
                    1f,
                    0.8f
                )
            );
        }
    }


    private void DrawDodge()
    {
        Vector2 direction =
            new Vector2(
                player.DodgeDirection,
                0f
            );

        DrawLine(
            -direction * 18f,
            direction * 32f,
            new Color(
                1f,
                1f,
                1f,
                0.5f
            ),
            6f
        );

        DrawCircle(
            -direction * 16f,
            17f,
            new Color(
                1f,
                1f,
                1f,
                0.12f
            )
        );
    }


    private void DrawHitVfx()
    {
        float progress =
            1f - Mathf.Clamp(
                player.HitVfxTimer / 0.12f,
                0f,
                1f
            );

        float radius =
            Mathf.Lerp(
                4f,
                20f,
                progress
            );

        float alpha =
            1f - progress;

        Vector2 position =
            player.HitVfxPosition;

        DrawCircle(
            position,
            radius,
            new Color(
                1f,
                1f,
                1f,
                alpha
            )
        );

        for (int i = 0; i < 6; i++)
        {
            float angle =
                Mathf.Tau * i / 6f;

            Vector2 direction =
                Vector2.FromAngle(angle);

            DrawLine(
                position +
                direction * 5f,

                position +
                direction *
                (10f + progress * 16f),

                new Color(
                    1f,
                    1f,
                    1f,
                    alpha
                ),

                3f
            );
        }
    }


    private void DrawStateFlash()
    {
        if (player.ParryFlashTimer > 0f)
        {
            DrawCircle(
                Vector2.Zero,
                32f,
                new Color(
                    1f,
                    1f,
                    1f,
                    0.4f
                )
            );
        }

        if (player.HitFlashTimer > 0f)
        {
            DrawCircle(
                Vector2.Zero,
                32f,
                new Color(
                    1f,
                    0.1f,
                    0.1f,
                    0.35f
                )
            );
        }
    }
}