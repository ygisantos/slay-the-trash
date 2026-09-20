using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionSystem : Singleton<ActionSystem>
{
    private List<GameAction> reactions = null;
    public bool IsPerforming { get; private set; } = false;
    private static Dictionary<Type, List<(Delegate source, Action<GameAction> wrapped)>> preSubs = new();
    private static Dictionary<Type, List<(Delegate source, Action<GameAction> wrapped)>> postSubs = new();
    private static Dictionary<Type, Func<GameAction, IEnumerator>> performers = new();

    public void Perform(GameAction action, System.Action OnPerformFinished = null)
    {
        if (IsPerforming) return;
        IsPerforming = true;
        StartCoroutine(Flow(action, () =>
        {
            IsPerforming = false;
            OnPerformFinished?.Invoke();
        }));
    }
    
    public void AddReaction(GameAction gameAction)
    {
        reactions?.Add(gameAction);
    }

    private IEnumerator Flow(GameAction action, Action OnFlowFinished = null)
    {
        reactions = action.PreReactions;
        PerformSubscribers(action, preSubs);
        yield return PerformReactions();

        reactions = action.PerformReactions;
        yield return PerformPerformer(action);
        yield return PerformReactions();
        reactions = action.PostReactions;
        PerformSubscribers(action, postSubs);
        yield return PerformReactions();

        OnFlowFinished?.Invoke();
    }

    private IEnumerator PerformPerformer (GameAction action)
    {
        Type type = action.GetType();
        if (performers.ContainsKey(type))
        {
            yield return performers[type](action);
        }
    }

    private void PerformSubscribers(GameAction action, Dictionary<Type, List<(Delegate source, Action<GameAction> wrapped)>> subs)
    {
        Type type = action.GetType();
        if (subs.ContainsKey(type))
        {
            foreach (var sub in subs[type].ToArray())
            {
                sub.wrapped(action);
            }
        }
    }

    private IEnumerator PerformReactions()
    {
        if (reactions == null) yield break;
        var pending = new List<GameAction>(reactions);
        foreach (var reaction in pending)
        {
            yield return Flow(reaction);
        }
    }

    public static void AttachPerformer<T>(Func<T, IEnumerator> performer) where T : GameAction
    {
        Type type = typeof(T);
        IEnumerator wrappedPerformer(GameAction action) => performer((T)action);
        if (performers.ContainsKey(type)) performers[type] = wrappedPerformer;
        else performers.Add(type, wrappedPerformer);
    }

    public static void DetachPerformer<T>() where T : GameAction
    {
        Type type = typeof(T);
        if (performers.ContainsKey(type)) performers.Remove(type);
    }

    public static void SubscribeReaction<T>(Action<T> reaction, ReactionTiming timing) where T : GameAction
    {
        Dictionary<Type, List<(Delegate source, Action<GameAction> wrapped)>> subs =
            timing == ReactionTiming.PRE ? preSubs : postSubs;
        Type type = typeof(T);
        if (!subs.ContainsKey(type))
            subs.Add(type, new());

        foreach (var item in subs[type])
        {
            if (item.source.Equals(reaction))
                return;
        }

        void wrappedReaction(GameAction action) => reaction((T)action);
        subs[type].Add((reaction, wrappedReaction));
    }
    public static void UnSubscribeReaction<T>(Action<T> reaction,  ReactionTiming timing) where T : GameAction
    {
        Dictionary<Type, List<(Delegate source, Action<GameAction> wrapped)>> subs =
            timing == ReactionTiming.PRE ? preSubs : postSubs;
        Type type = typeof(T);
        if (!subs.ContainsKey(type)) return;
        subs[type].RemoveAll(item => item.source.Equals(reaction));
    }
}
