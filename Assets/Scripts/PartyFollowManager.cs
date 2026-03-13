using UnityEngine;
using System.Collections.Generic;

public class PartyManager : MonoBehaviour
{
    [Header("Party Members")]
    public GameObject player;
    public List<GameObject> partyMembers; // The follower GameObjects

    [Header("Follow Settings")]
    public float spacing = 1.5f; // Space between each follower

    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        // Set up follow chain
        SetupFollowChain();
    }

    private void SetupFollowChain()
    {
        if (partyMembers == null || partyMembers.Count == 0) return;

        // First follower follows player
        PartyFollower firstFollower = partyMembers[0].GetComponent<PartyFollower>();
        if (firstFollower != null)
        {
            firstFollower.target = player.transform;
            firstFollower.followDistance = spacing;
        }

        // Subsequent followers follow the previous follower
        for (int i = 1; i < partyMembers.Count; i++)
        {
            PartyFollower follower = partyMembers[i].GetComponent<PartyFollower>();
            if (follower != null)
            {
                follower.target = partyMembers[i - 1].transform;
                follower.followDistance = spacing;
            }
        }
    }

    // Call this to add a new party member
    public void AddPartyMember(GameObject newMember)
    {
        if (!partyMembers.Contains(newMember))
        {
            partyMembers.Add(newMember);
            SetupFollowChain();
        }
    }

    // Call this to remove a party member
    public void RemovePartyMember(GameObject member)
    {
        if (partyMembers.Contains(member))
        {
            partyMembers.Remove(member);
            SetupFollowChain();
        }
    }
}