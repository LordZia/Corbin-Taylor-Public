using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NPCs
{
    public interface MovementController
    {

    }

    public interface AttackController
    {
        void SendAttackOneShot();
        void SendAttackContinuous();
    }
}

