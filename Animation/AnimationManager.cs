using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Animation
{
    [RequireComponent(typeof(Animator))]
    public class AnimationManager : MonoBehaviour , IAnimationManager
    {
        [SerializeField] private Animator _animator;

        private void Awake()
        {
            if (_animator == null )
                _animator = this.GetComponent<Animator>();

            OnAwake();
        }

        protected virtual void OnAwake()
        {
        }

        public void PlayAnimation(string animationName)
        {
            Debug.Log($"I {this.gameObject.name} recieved a call to play the animation {animationName}");
            _animator.Play(animationName);
        }

        public void SetBool(string parameterName, bool value)
        {
            _animator.SetBool(parameterName, value);
        }

        public void SetTrigger(string parameterName)
        {
            _animator.SetTrigger(parameterName);
        }

    }

    public interface IAnimationManager
    {
        void PlayAnimation(string animationName);
        void SetBool(string parameterName, bool value);
        void SetTrigger(string parameterName);
    }
}
