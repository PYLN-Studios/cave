using Interactables;
using Mirror;
using StarterAssets;
using UnityEngine;

public class PlayerCollecting : NetworkBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float maxRaycastDistance = 5f;

    private StarterAssetsInputs _input;
    private GameObject _mainCamera;

    public override void OnStartLocalPlayer()
    {
        _input = GetComponent<StarterAssetsInputs>();

        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        _input = GetComponent<StarterAssetsInputs>();
    }

    [Client]
    private void Update()
    {
        if (!isLocalPlayer) return;
        if (_input == null) return;

        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            if (_mainCamera == null) return;
        }

        // look for the closest interactable object in front of the player
        // if the player presses the interact button, call the interact method on the object
        Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance))
        {
            return;
        }

        IInteractable interactable = hit.collider.GetComponent<IInteractable>();
        if (interactable == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, hit.collider.transform.position);
        if (distance > interactable.InteractRange)
        {
            return;
        }

        interactable.OnHover();

        if (_input.use)
        {
            _input.use = false;
            CmdInteract(hit.collider.gameObject);
        }
    }

    [Command]
    private void CmdInteract(GameObject target)
    {
        if (target == null) return;

        if (!target.TryGetComponent(out IInteractable interactable))
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > interactable.InteractRange)
        {
            return;
        }

        interactable.OnInteract(gameObject);
        NetworkServer.Destroy(target);
    }
}
