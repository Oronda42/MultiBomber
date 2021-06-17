﻿using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerDiseaseManager : NetworkBehaviour
{
    public Timer diseaseTimer;
    public Disease currentDisease;
    PlayerEntity player;

    public void Init(PlayerEntity player)
    {
        this.player = player;
        diseaseTimer = GetComponent<Timer>();
        diseaseTimer.onTimerEnd += EndDisease;
    }

   
    public void StartDisease(Disease disease, float duration)
    {
        RpcStartDisease(disease,  duration);
    }

    [ClientRpc]
    private void RpcStartDisease(Disease disease, float duration)
    {
        if (currentDisease != null)
                    EndDisease();
        
        currentDisease = disease;
        diseaseTimer.StopTimer();
        diseaseTimer.StartTimer(duration);
        disease.PerformAction(player);
        Debug.Log("Start of " + currentDisease.ToString());
    }

    public void EndDisease()
    {
        if (isServer)
        {
            RpcEndDisease();
        }
      
    }

    [ClientRpc]
    public void RpcEndDisease()
    {
        if (currentDisease != null)
        {
            Debug.Log("End of " + currentDisease.ToString());
            currentDisease.UnPerformAction(player);
            currentDisease = null;
        }
        else
        {
            Debug.LogWarning("End of currentDisease = NULL");
        }
        
    }

    public void SpreadDisease(PlayerEntity playerTarget)
    {
        float elapsedDiseaseTime = Time.time - diseaseTimer.elapsedTime;

        if (playerTarget.playerDiseaseManager.currentDisease!=null)
        {
            playerTarget.playerDiseaseManager.EndDisease();
        }

        playerTarget.playerDiseaseManager.StartDisease(currentDisease, elapsedDiseaseTime);

    }
}
   


