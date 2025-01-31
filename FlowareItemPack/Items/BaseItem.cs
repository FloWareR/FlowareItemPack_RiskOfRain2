using RoR2;

public abstract class BaseItem
{
    public abstract ItemDef ItemDef { get; }
    public abstract void Initialize();
    public virtual void Hook() { } 
    public virtual void Unhook() { }
}
