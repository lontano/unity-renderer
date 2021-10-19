using DCL.Controllers;
using System.Collections;
using System.Collections.Generic;
using DCL;
using UnityEngine;

public class BuilderInWorldAudioHandler : MonoBehaviour
{
    const float MUSIC_DELAY_TIME_ON_START = 4f;
    const float MUSIC_FADE_OUT_TIME_ON_EXIT = 5f;
    const float MUSIC_FADE_OUT_TIME_ON_TUTORIAL = 3f;

    private IBIWCreatorController creatorController;

    private IBIWEntityHandler entityHandler;

    private IBIWModeController modeController;

    [Header("Audio Events")]
    [SerializeField]
    AudioEvent eventAssetSpawn;

    [SerializeField]
    AudioEvent eventAssetPlace;

    [SerializeField]
    AudioEvent eventAssetSelect;

    [SerializeField]
    AudioEvent eventAssetDeselect;

    [SerializeField]
    AudioEvent eventBuilderOutOfBounds;

    [SerializeField]
    AudioEvent eventBuilderOutOfBoundsPlaced;

    [SerializeField]
    AudioEvent eventAssetDelete;

    [SerializeField]
    AudioEvent eventBuilderExit;

    [SerializeField]
    AudioEvent eventBuilderMusic;

    private List<string> entitiesOutOfBounds = new List<string>();
    private int entityCount;
    bool playPlacementSoundOnDeselect;
    private BIWModeController.EditModeState state = BIWModeController.EditModeState.Inactive;

    private Coroutine fadeOutCoroutine;
    private Coroutine startBuilderMusicCoroutine;

    private void Start() { playPlacementSoundOnDeselect = false; }

    public void Initialize(BIWContext context)
    {
        creatorController = context.creatorController;
        entityHandler = context.entityHandler;
        modeController = context.modeController;

        AddListeners();
    }

    private void AddListeners()
    {
        creatorController.OnCatalogItemPlaced += OnAssetSpawn;
        entityHandler.OnDeleteSelectedEntities += OnAssetDelete;
        modeController.OnChangedEditModeState += OnChangedEditModeState;
        DCL.Environment.i.world.sceneBoundsChecker.OnEntityBoundsCheckerStatusChanged += OnEntityBoundsCheckerStatusChanged;

        if (DCL.Tutorial.TutorialController.i != null)
        {
            DCL.Tutorial.TutorialController.i.OnTutorialEnabled += OnTutorialEnabled;
            DCL.Tutorial.TutorialController.i.OnTutorialDisabled += OnTutorialDisabled;
        }

        entityHandler.OnEntityDeselected += OnAssetDeselect;
        entityHandler.OnEntitySelected += OnAssetSelect;
    }

    public void EnterEditMode(ParcelScene scene)
    {
        UpdateEntityCount();

        if (eventBuilderMusic.source.gameObject.activeSelf)
        {
            if (startBuilderMusicCoroutine != null)
                CoroutineStarter.Stop(startBuilderMusicCoroutine);

            startBuilderMusicCoroutine = CoroutineStarter.Start(StartBuilderMusic());
        }

        if (HUDController.i.builderInWorldMainHud != null)
            HUDController.i.builderInWorldMainHud.OnCatalogItemSelected += OnCatalogItemSelected;

        gameObject.SetActive(true);
    }

    public void ExitEditMode()
    {
        eventBuilderExit.Play();
        if (eventBuilderMusic.source.gameObject.activeSelf)
        {
            if (fadeOutCoroutine != null)
                CoroutineStarter.Stop(fadeOutCoroutine);
            fadeOutCoroutine = CoroutineStarter.Start(eventBuilderMusic.FadeOut(MUSIC_FADE_OUT_TIME_ON_EXIT));
        }

        if (HUDController.i.builderInWorldMainHud != null)
            HUDController.i.builderInWorldMainHud.OnCatalogItemSelected -= OnCatalogItemSelected;

        gameObject.SetActive(false);
    }

    private void OnAssetSpawn() { eventAssetSpawn.Play(); }

    private void OnAssetDelete(List<BIWEntity> entities)
    {
        foreach (BIWEntity deletedEntity in entities)
        {
            if (entitiesOutOfBounds.Contains(deletedEntity.rootEntity.entityId))
            {
                entitiesOutOfBounds.Remove(deletedEntity.rootEntity.entityId);
            }
        }

        eventAssetDelete.Play();
    }

    private void OnAssetSelect() { eventAssetSelect.Play(); }

    private void OnAssetDeselect(BIWEntity entity)
    {
        if (playPlacementSoundOnDeselect)
        {
            eventAssetPlace.Play();
            playPlacementSoundOnDeselect = false;
        }
        else
            eventAssetDeselect.Play();

        UpdateEntityCount();

        if (entitiesOutOfBounds.Contains(entity.rootEntity.entityId))
        {
            eventBuilderOutOfBoundsPlaced.Play();
        }
    }

    private void OnCatalogItemSelected(CatalogItem catalogItem) { playPlacementSoundOnDeselect = true; }

    private void OnTutorialEnabled()
    {
        if (gameObject.activeInHierarchy)
        {
            if (fadeOutCoroutine != null)
                CoroutineStarter.Stop(fadeOutCoroutine);
            fadeOutCoroutine =  CoroutineStarter.Start(eventBuilderMusic.FadeOut(MUSIC_FADE_OUT_TIME_ON_TUTORIAL));
        }
    }

    private void OnTutorialDisabled()
    {
        if (gameObject.activeInHierarchy)
        {
            if (startBuilderMusicCoroutine != null)
                CoroutineStarter.Stop(startBuilderMusicCoroutine);
            startBuilderMusicCoroutine = CoroutineStarter.Start(StartBuilderMusic());
        }
    }

    private IEnumerator StartBuilderMusic()
    {
        yield return new WaitForSeconds(MUSIC_DELAY_TIME_ON_START);

        if (gameObject != null && gameObject.activeInHierarchy)
            eventBuilderMusic.Play();
    }

    private void OnChangedEditModeState(BIWModeController.EditModeState previous, BIWModeController.EditModeState current)
    {
        state = current;
        if (previous != BIWModeController.EditModeState.Inactive)
        {
            switch (current)
            {
                case BIWModeController.EditModeState.FirstPerson:
                    AudioScriptableObjects.cameraFadeIn.Play();
                    break;
                case BIWModeController.EditModeState.GodMode:
                    AudioScriptableObjects.cameraFadeOut.Play();
                    break;
                default:
                    break;
            }
        }
    }

    private void OnEntityBoundsCheckerStatusChanged(DCL.Models.IDCLEntity entity, bool isInsideBoundaries)
    {
        if (state == BIWModeController.EditModeState.Inactive)
            return;

        if (!isInsideBoundaries)
        {
            if (!entitiesOutOfBounds.Contains(entity.entityId))
            {
                entitiesOutOfBounds.Add(entity.entityId);
                eventBuilderOutOfBounds.Play();
            }
        }
        else
        {
            if (entitiesOutOfBounds.Contains(entity.entityId))
            {
                entitiesOutOfBounds.Remove(entity.entityId);
            }
        }
    }

    private void UpdateEntityCount() { entityCount = entityHandler.GetCurrentSceneEntityCount(); }

    private bool EntityHasBeenAddedSinceLastUpdate() { return (entityHandler.GetCurrentSceneEntityCount() > entityCount); }

    private void OnDestroy() { Dispose(); }

    public void Dispose()
    {
        if (startBuilderMusicCoroutine != null)
            CoroutineStarter.Stop(startBuilderMusicCoroutine);

        if (fadeOutCoroutine != null)
            StopCoroutine(fadeOutCoroutine);

        startBuilderMusicCoroutine = null;
        fadeOutCoroutine = null;

        RemoveListeners();
    }

    private void RemoveListeners()
    {
        creatorController.OnCatalogItemPlaced -= OnAssetSpawn;
        entityHandler.OnDeleteSelectedEntities -= OnAssetDelete;
        modeController.OnChangedEditModeState -= OnChangedEditModeState;
        DCL.Environment.i.world.sceneBoundsChecker.OnEntityBoundsCheckerStatusChanged -= OnEntityBoundsCheckerStatusChanged;

        if (DCL.Tutorial.TutorialController.i != null)
        {
            DCL.Tutorial.TutorialController.i.OnTutorialEnabled -= OnTutorialEnabled;
            DCL.Tutorial.TutorialController.i.OnTutorialDisabled -= OnTutorialDisabled;
        }

        entityHandler.OnEntityDeselected -= OnAssetDeselect;
        entityHandler.OnEntitySelected -= OnAssetSelect;
    }
}