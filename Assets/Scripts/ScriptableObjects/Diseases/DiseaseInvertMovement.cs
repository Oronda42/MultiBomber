﻿using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableAction/Diseases/DiseaseInvertMovement", fileName = "DiseaseInvertMovement")]

public class DiseaseInvertMovement : Disease
{
    public Filter filter;

    public override void PerformAction(PlayerEntity player)
    {
        player.playerMovement.currentFilter = filter;
    }

    public override void UnPerformAction(PlayerEntity player)
    {
        player.playerMovement.currentFilter = null;
    }

}

