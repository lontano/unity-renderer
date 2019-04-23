using System.Collections;
using System.Collections.Generic;
using DCL.Components;
using DCL.Helpers;
using DCL.Models;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests
{
    public class UIContainerRectTests : TestsBase
    {
        [UnityTest]
        public IEnumerator TestPropertiesAreAppliedCorrectly()
        {
            yield return InitScene();

            DCLCharacterController.i.gravity = 0f;

            // Position character inside parcel (0,0)
            DCLCharacterController.i.SetPosition(JsonConvert.SerializeObject(
                new
                {
                    x = 0f,
                    y = 0f,
                    z = 0f
                }));

            // Create UIScreenSpaceShape
            UIScreenSpace screenSpaceShape = TestHelpers.SharedComponentCreate<UIScreenSpace, UIScreenSpace.Model>(scene, CLASS_ID.UI_SCREEN_SPACE_SHAPE);

            yield return screenSpaceShape.routine;

            // Create UIContainerRectShape
            UIContainerRect uiContainerRectShape = TestHelpers.SharedComponentCreate<UIContainerRect, UIContainerRect.Model>(scene, CLASS_ID.UI_CONTAINER_RECT);

            yield return uiContainerRectShape.routine;

            UnityEngine.UI.Image image = uiContainerRectShape.referencesContainer.image;

            // Check default properties are applied correctly
            Assert.IsTrue(image.GetComponent<Outline>() == null);
            Assert.IsTrue(image.color == new Color(0f, 0f, 0f, 0f));
            Assert.IsTrue(uiContainerRectShape.referencesContainer.canvasGroup.blocksRaycasts);
            Assert.AreEqual(100f, uiContainerRectShape.childHookRectTransform.rect.width);
            Assert.AreEqual(100f, uiContainerRectShape.childHookRectTransform.rect.height);
            Assert.AreEqual(Vector3.zero, uiContainerRectShape.childHookRectTransform.localPosition);

            // Update UIContainerRectShape properties
            uiContainerRectShape = scene.SharedComponentUpdate(JsonUtility.ToJson(new SharedComponentUpdateMessage
            {
                id = uiContainerRectShape.id,
                json = JsonUtility.ToJson(new UIContainerRect.Model
                {
                    parentComponent = screenSpaceShape.id,
                    thickness = 5,
                    color = new Color(0.2f, 0.7f, 0.05f, 1f),
                    isPointerBlocker = false,
                    width = new UIValue(275f),
                    height = new UIValue(130f),
                    positionX = new UIValue(-30f),
                    positionY = new UIValue(-15f),
                    hAlign = "right",
                    vAlign = "bottom"
                })
            })) as UIContainerRect;

            yield return uiContainerRectShape.routine;

            // Check updated properties are applied correctly
            Assert.IsTrue(uiContainerRectShape.referencesContainer.transform.parent == screenSpaceShape.childHookRectTransform);
            Assert.IsTrue(image.GetComponent<Outline>() != null);
            Assert.IsTrue(image.color == new Color(0.2f, 0.7f, 0.05f, 1f));
            Assert.IsFalse(uiContainerRectShape.referencesContainer.canvasGroup.blocksRaycasts);
            Assert.AreEqual(275f, uiContainerRectShape.childHookRectTransform.rect.width);
            Assert.AreEqual(130f, uiContainerRectShape.childHookRectTransform.rect.height);
            Assert.IsTrue(uiContainerRectShape.referencesContainer.layoutGroup.childAlignment == TextAnchor.LowerRight);

            Vector2 alignedPosition = CalculateAlignedPosition(screenSpaceShape.childHookRectTransform.rect, uiContainerRectShape.childHookRectTransform.rect, "bottom", "right");
            alignedPosition += new Vector2(-30, -15); // apply offset position

            Assert.AreEqual(alignedPosition.ToString(), uiContainerRectShape.childHookRectTransform.anchoredPosition.ToString());

            screenSpaceShape.Dispose();
        }

        [UnityTest]
        public IEnumerator TestParenting()
        {
            yield return InitScene();

            DCLCharacterController.i.gravity = 0f;

            // Position character inside parcel (0,0)
            DCLCharacterController.i.SetPosition(JsonConvert.SerializeObject(new
            {
                x = 0f,
                y = 0f,
                z = 0f
            }));

            // Create UIScreenSpaceShape
            UIScreenSpace screenSpaceShape = TestHelpers.SharedComponentCreate<UIScreenSpace, UIScreenSpace.Model>(scene, CLASS_ID.UI_SCREEN_SPACE_SHAPE);
            yield return screenSpaceShape.routine;

            // Create UIContainerRectShape
            UIContainerRect uiContainerRectShape = TestHelpers.SharedComponentCreate<UIContainerRect, UIContainerRect.Model>(scene, CLASS_ID.UI_CONTAINER_RECT);
            yield return uiContainerRectShape.routine;

            // Update UIContainerRectShape parent
            scene.SharedComponentUpdate(JsonUtility.ToJson(new SharedComponentUpdateMessage
            {
                id = uiContainerRectShape.id,
                json = JsonUtility.ToJson(new UIContainerRect.Model
                {
                    parentComponent = screenSpaceShape.id,
                })
            }));
            yield return uiContainerRectShape.routine;

            // Check updated parent
            Assert.IsTrue(uiContainerRectShape.referencesContainer.transform.parent == screenSpaceShape.childHookRectTransform);

            // Create 2nd UIContainerRectShape
            UIContainerRect uiContainerRectShape2 = TestHelpers.SharedComponentCreate<UIContainerRect, UIContainerRect.Model>(scene, CLASS_ID.UI_CONTAINER_RECT);
            yield return uiContainerRectShape2.routine;

            // Update UIContainerRectShape parent to the previous container
            scene.SharedComponentUpdate(JsonUtility.ToJson(new SharedComponentUpdateMessage
            {
                id = uiContainerRectShape2.id,
                json = JsonUtility.ToJson(new UIContainerRect.Model
                {
                    parentComponent = uiContainerRectShape.id,
                })
            }));
            yield return uiContainerRectShape2.routine;

            // Check updated parent
            Assert.IsTrue(uiContainerRectShape2.referencesContainer.transform.parent == uiContainerRectShape.childHookRectTransform);

            screenSpaceShape.Dispose();
        }

        [UnityTest]
        public IEnumerator TestMissingValuesGetDefaultedOnUpdate()
        {
            yield return InitScene();

            //// Create UIScreenSpaceShape
            UIScreenSpace screenSpaceShape = TestHelpers.SharedComponentCreate<UIScreenSpace, UIScreenSpace.Model>(scene, CLASS_ID.UI_SCREEN_SPACE_SHAPE);
            yield return screenSpaceShape.routine;

            Assert.IsFalse(screenSpaceShape == null);

            yield return TestHelpers.TestSharedComponentDefaultsOnUpdate<UIContainerRect.Model, UIContainerRect>(scene, CLASS_ID.UI_CONTAINER_RECT);
        }

        [UnityTest]
        public IEnumerator TestNormalizedSize()
        {
            yield return InitScene();

            DCLCharacterController.i.gravity = 0f;

            // Position character inside parcel (0,0)
            DCLCharacterController.i.SetPosition(JsonConvert.SerializeObject(new
            {
                x = 0f,
                y = 0f,
                z = 0f
            }));

            // Create UIScreenSpaceShape
            UIScreenSpace screenSpaceShape = TestHelpers.SharedComponentCreate<UIScreenSpace, UIScreenSpace.Model>(scene, CLASS_ID.UI_SCREEN_SPACE_SHAPE);
            yield return screenSpaceShape.routine;

            // Create UIContainerRectShape
            UIContainerRect uiContainerRectShape = TestHelpers.SharedComponentCreate<UIContainerRect, UIContainerRect.Model>(scene, CLASS_ID.UI_CONTAINER_RECT);
            yield return uiContainerRectShape.routine;

            // Update UIContainerRectShape properties
            uiContainerRectShape = scene.SharedComponentUpdate(JsonUtility.ToJson(new SharedComponentUpdateMessage
            {
                id = uiContainerRectShape.id,
                json = JsonUtility.ToJson(new UIContainerRect.Model
                {
                    parentComponent = screenSpaceShape.id,
                    width = new UIValue(50, UIValue.Unit.PERCENT),
                    height = new UIValue(30, UIValue.Unit.PERCENT)
                })
            })) as UIContainerRect;
            yield return uiContainerRectShape.routine;

            UnityEngine.UI.Image image = uiContainerRectShape.childHookRectTransform.GetComponent<UnityEngine.UI.Image>();

            // Check updated properties are applied correctly
            Assert.AreEqual(screenSpaceShape.childHookRectTransform.rect.width * 0.5f, uiContainerRectShape.childHookRectTransform.rect.width);
            Assert.AreEqual(screenSpaceShape.childHookRectTransform.rect.height * 0.3f, uiContainerRectShape.childHookRectTransform.rect.height);

            screenSpaceShape.Dispose();
        }
    }
}
