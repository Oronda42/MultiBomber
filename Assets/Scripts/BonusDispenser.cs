using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BonusDispenser
{
    public SO_LevelBonusSettings bonusSettings;

    public void AssignBonus(ref List<ItemBox> boxList, int playersCount)
    {
        bonusSettings.boxesCount = boxList.Count;
        bonusSettings.playersCount = playersCount;

        int bonusCount = Mathf.RoundToInt(boxList.Count * bonusSettings.bonusPercentage);

        SO_LevelBonusSettings.BonusStock[] bonusStocks = bonusSettings.ComputeBonusList(bonusCount, playersCount);
        
        System.Random rnd = new System.Random();
        
        int[] indices = new int[boxList.Count];
        for(int i = 0 ; i < boxList.Count ; i++)
        {
            indices[i] = i;
        }
       
        // randomize boxes index list
        rnd.Shuffle(indices);
       
        // assign all bonus into boxes
        int pickIndex = 0;
        int bonusLeft = 1;
        while (pickIndex < indices.Length && bonusLeft > 0)
        {
            bonusLeft = 0;
            for (int i = 0 ; i < bonusStocks.Length ; i++)
            {
                if (bonusStocks[i].count > 0)
                {
                    boxList[indices[pickIndex]].bonus = bonusStocks[i].bonus;
                    bonusStocks[i].count--;
                    pickIndex++;
                    bonusLeft += bonusStocks[i].count;

                    // early exit
                    if (pickIndex >= indices.Length) break;
                }
            }
        }
    }
}
