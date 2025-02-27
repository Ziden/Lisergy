using ClientSDK;
using Game.ECS;
using Game.Engine.ECS;
using Game.Engine.Events.Bus;

public interface IComponentListener : IEventListener { }

public interface ISpecificComponentListener<ComponentType> : IComponentListener where ComponentType : IComponent 
{
    /// <summary>
    /// Called whenever the component is updated on any given entity
    /// </summary>
    void OnUpdateComponent(IEntity entity, ComponentType oldComponent, ComponentType? newComponent);
}

public abstract class BaseComponentListener<ComponentType> : ISpecificComponentListener<ComponentType> where ComponentType : IComponent
{
    protected IGameClient GameClient { get; private set; }

    public BaseComponentListener(IGameClient client)
    {
        GameClient = client;
        GameClient.Modules.Components.OnComponentUpdate<ComponentType>(OnUpdateComponent);
        GameClient.Modules.Components.OnComponentRemoved<ComponentType>(OnComponentRemoved);
    }

    public abstract void OnUpdateComponent(IEntity entity, ComponentType oldComponent, ComponentType? newComponent);

    public virtual void OnComponentRemoved(IEntity entity, ComponentType oldComponent) { }
}