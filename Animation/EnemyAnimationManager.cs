using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace Animation
{
    [RequireComponent(typeof(EnemyController))]
    public class EnemyAnimationManager : AnimationEventManager<EnemyController, EnemyState>
    {
        // Any additional logic specific to the EnemyAnimationManager can be added here
    }
}

