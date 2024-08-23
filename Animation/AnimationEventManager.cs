using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Animation
{
    public class AnimationEventManager<TController, TState> : AnimationManager
        where TController : MonoBehaviour, IStateChangeNotifier<TState>
    {
        [SerializeField] private TController _controller;
        [SerializeField] private List<AnimationOnEvent<TState>> animationOnEvents = new List<AnimationOnEvent<TState>>();

        private TState currentState;

        protected override void OnAwake()
        {
            if (_controller == null)
                _controller = this.GetComponent<TController>();

            _controller.OnStateChange += OnStateChange;
            currentState = _controller.GetCurrentState();
        }

        private void OnDestroy()
        {
            if (_controller != null)
                _controller.OnStateChange -= OnStateChange;
        }

        private void OnStateChange(TState newState)
        {
            if (currentState.Equals(newState))
                return;

            currentState = newState;

            var matchingEvent = animationOnEvents.FirstOrDefault(e => e.stateTrigger.Equals(currentState));

            base.PlayAnimation(matchingEvent.animationName);                
        }
    }

    [System.Serializable]
    public struct AnimationOnEvent<TState>
    {
        [SerializeField] public TState stateTrigger;
        [SerializeField] public string animationName;
    }
    
}