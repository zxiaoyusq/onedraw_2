namespace OneStrokeDemon.Combat
{
    public interface IHittable
    {
        int HitTargetId { get; }

        bool CanReceiveStrokeHit { get; }
    }

    public interface IStrokeHitbox
    {
        IHittable HitTarget { get; }

        bool IsWeakpoint { get; }

        bool IsStrokeHitboxActive { get; }
    }
}
