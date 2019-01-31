import { BaseComponent } from '../BaseComponent'
import { scene } from 'engine/renderer'
import { BaseEntity } from 'engine/entities/BaseEntity'

export enum Gizmo {
  MOVE = 'MOVE',
  ROTATE = 'ROTATE',
  SCALE = 'SCALE'
}

export const gizmoManager = new BABYLON.GizmoManager(scene)
gizmoManager.positionGizmoEnabled = true
gizmoManager.rotationGizmoEnabled = true
gizmoManager.scaleGizmoEnabled = true

gizmoManager.usePointerToAttachGizmos = false

let activeEntity: BaseEntity = null
let selectedGizmo: Gizmo = Gizmo.MOVE

function switchGizmo() {
  // TODO enable gizmo switching
  // 1 return selectedGizmo === 'translate' ? 'rotate' : 'translate'
  return Gizmo.MOVE
}

{
  let tmpMatrix = new BABYLON.Matrix()

  let initialRotation: BABYLON.Quaternion

  gizmoManager.gizmos.positionGizmo.onDragEndObservable.add(() => {
    let final = activeEntity.position.clone()
    let deltaPosition = new BABYLON.Vector3()
    activeEntity.computeWorldMatrix().invertToRef(tmpMatrix)
    tmpMatrix.setTranslationFromFloats(0, 0, 0)

    const delta = activeEntity.getObject3D('gizmoMesh').position
    BABYLON.Vector3.TransformCoordinatesToRef(delta, tmpMatrix, deltaPosition)
    delta.set(0, 0, 0)

    activeEntity.dispatchUUIDEvent('gizmoEvent', {
      type: 'gizmoDragEnded',
      transform: {
        position: final.addInPlace(deltaPosition),
        rotation: activeEntity.rotationQuaternion,
        scale: activeEntity.scaling
      },
      entityId: activeEntity.uuid
    })
  })

  gizmoManager.gizmos.rotationGizmo.onDragStartObservable.add(() => {
    const gizmoMesh = (activeEntity.getObject3D('gizmoMesh') as BABYLON.AbstractMesh) || null

    if (!gizmoMesh) return
    if (!gizmoMesh.rotationQuaternion) {
      gizmoMesh.rotationQuaternion = BABYLON.Quaternion.RotationYawPitchRoll(
        gizmoMesh.rotation.y,
        gizmoMesh.rotation.x,
        gizmoMesh.rotation.z
      )
    }

    initialRotation = gizmoMesh.rotationQuaternion
  })

  gizmoManager.gizmos.rotationGizmo.onDragEndObservable.add(() => {
    const gizmoMesh = (activeEntity.getObject3D('gizmoMesh') as BABYLON.AbstractMesh) || null

    if (!gizmoMesh) return

    const finalRotation = gizmoMesh.rotationQuaternion.clone()

    gizmoMesh.rotationQuaternion.copyFrom(initialRotation)

    activeEntity.dispatchUUIDEvent('gizmoEvent', {
      type: 'gizmoDragEnded',
      transform: {
        position: activeEntity.position,
        rotation: finalRotation,
        scale: activeEntity.scaling
      },
      entityId: activeEntity.uuid
    })
  })
}

export function selectGizmo(type: Gizmo | null) {
  const gizmoMesh = (activeEntity.getObject3D('gizmoMesh') as BABYLON.AbstractMesh) || null

  selectedGizmo = type

  if (type === Gizmo.MOVE) {
    gizmoManager.gizmos.positionGizmo.attachedMesh = gizmoMesh
    gizmoManager.gizmos.rotationGizmo.attachedMesh = null
    gizmoManager.gizmos.scaleGizmo.attachedMesh = null
  } else if (type === Gizmo.ROTATE) {
    gizmoManager.gizmos.positionGizmo.attachedMesh = null
    gizmoManager.gizmos.rotationGizmo.attachedMesh = gizmoMesh
    gizmoManager.gizmos.scaleGizmo.attachedMesh = null
  } else if (type === Gizmo.SCALE) {
    gizmoManager.gizmos.positionGizmo.attachedMesh = null
    gizmoManager.gizmos.rotationGizmo.attachedMesh = null
    gizmoManager.gizmos.scaleGizmo.attachedMesh = gizmoMesh
  } else {
    gizmoManager.gizmos.positionGizmo.attachedMesh = null
    gizmoManager.gizmos.rotationGizmo.attachedMesh = null
    gizmoManager.gizmos.scaleGizmo.attachedMesh = null
  }
}

export class Gizmos extends BaseComponent<any> {
  transformValue(data: any) {
    return {
      visible: !!data.visible,
      currentGizmo: data.currentGizmo
    }
  }

  update() {
    // stub
  }

  attach(entity: BaseEntity) {
    const gizmoMesh = BABYLON.MeshBuilder.CreateBox('gizmoMesh', {})
    gizmoMesh.visibility = 0
    entity.setObject3D('gizmoMesh', gizmoMesh)
    entity.addListener('onClick', () => {
      if (entity === activeEntity) {
        selectGizmo(switchGizmo())
      } else {
        activeEntity = entity
        selectGizmo(selectedGizmo)
      }

      activeEntity.dispatchUUIDEvent('gizmoEvent', {
        type: 'gizmoSelected',
        gizmoType: selectedGizmo,
        entityId: entity.uuid
      })
    })
  }

  detach() {
    this.entity.removeObject3D('gizmoMesh')
  }
}
