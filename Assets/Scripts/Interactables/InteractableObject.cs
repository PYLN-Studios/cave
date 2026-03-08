using System.Collections.Generic;
using UnityEngine;
using Mirror;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/guides/networkbehaviour
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkBehaviour.html
*/

/*
 * Any objects that the player can "interact" with using the interact button should inherit from this interface.
 * E.g. picking up items, campfire
 */
namespace Interactables
{
    // name is so sad :(
    public interface IInteractable
    {
        /// <summary>
        /// GetInteractRange returns the distance at which the player can interact with the object.
        /// </summary>
        float InteractRange { get; set; }
        
        /// <summary>
        /// Called when the player is in range to interact with the object.
        /// This could be used to show a UI prompt, highlight the object, etc.
        /// </summary>
        /// <returns>1 if the object can be interacted with, 0 otherwise</returns>
        int OnHover();

        /// <summary>
        /// Called when the player presses the interact button to perform the interaction.
        /// This could be picking up an item, lighting a campfire, etc.
        /// </summary>
        /// <param name="player">The player GameObject interacting with the object.</param>
        /// <returns>0 if the interaction was successful, and non-zero otherwise</returns>
        int OnInteract(GameObject player);
    }
}
