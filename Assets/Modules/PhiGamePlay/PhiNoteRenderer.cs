using System;
using System.Collections.Generic;
using System.Numerics;
using Klrohias.NFast.PhiChartLoader;
using Klrohias.NFast.UIComponent;
using Klrohias.NFast.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiNoteRenderer : MonoBehaviour
    {
        [Serializable]
        public struct NoteResourcesPrototype
        {
            public Texture2D TapTexture2D;
            public Texture2D DragTexture2D;
            public Texture2D FlickTexture2D;
        }

        public Material NoteMaterial;
        public Camera TargetCamera;
        public ScreenAdapter Adapter;
        public int TargetLayer = 6;
        public float NoteZPos = 7.5f;
        public Vector3 NoteScale;
        public NoteResourcesPrototype NoteResources;

        private Mesh _mesh;

        private MaterialPropertyBlock _tapNotePropertyBlock;
        private MaterialPropertyBlock _dragNotePropertyBlock;
        private MaterialPropertyBlock _flickNotePropertyBlock;

        private UnorderedList<PhiNote> _runningNotes = new();
        private readonly UnorderedList<Matrix4x4> _tapMatrixArray = new();
        private readonly UnorderedList<Matrix4x4> _dragMatrixArray = new();
        private readonly UnorderedList<Matrix4x4> _flickMatrixArray = new();
        private void Initialize()
        {
            GenerateMesh();

            // TAG: note scale
            var noteScale = NoteScale * Adapter.ScaleFactor;
            noteScale.Log();

            _tapMatrixArray.Add(Matrix4x4.TRS(Vector3.forward * NoteZPos + Vector3.left * 9, Quaternion.Euler(0, 0, 0), noteScale)); // TODO test

            // generate property blocks
            _tapNotePropertyBlock = GenerateMaterialPropertyBlock(NoteResources.TapTexture2D);
            _dragNotePropertyBlock = GenerateMaterialPropertyBlock(NoteResources.DragTexture2D);
            _flickNotePropertyBlock = GenerateMaterialPropertyBlock(NoteResources.FlickTexture2D);
        }

        private MaterialPropertyBlock GenerateMaterialPropertyBlock(Texture2D texture)
        {
            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetTexture("_MainTex", texture);
            return propertyBlock;
        }

        private void CommitRender()
        {
            lock (_tapMatrixArray)
            {
                Graphics.DrawMeshInstanced(_mesh, 0, NoteMaterial, _tapMatrixArray.GetInternalCollection(),
                    _tapMatrixArray.Length, _tapNotePropertyBlock,
                    ShadowCastingMode.Off, false, TargetLayer, TargetCamera);
            }

            lock (_dragMatrixArray)
            {
                Graphics.DrawMeshInstanced(_mesh, 0, NoteMaterial, _dragMatrixArray.GetInternalCollection(),
                    _dragMatrixArray.Length, _dragNotePropertyBlock,
                    ShadowCastingMode.Off, false, TargetLayer, TargetCamera);
            }

            lock (_flickMatrixArray)
            {
                Graphics.DrawMeshInstanced(_mesh, 0, NoteMaterial, _flickMatrixArray.GetInternalCollection(),
                    _flickMatrixArray.Length, _flickNotePropertyBlock,
                    ShadowCastingMode.Off, false, TargetLayer, TargetCamera);
            }
        }

        private void GenerateMesh() =>
            _mesh = new Mesh
            {
                vertices = new Vector3[]
                {
                    new(1, 1, 0),
                    new(-1, 1, 0),
                    new(1, -1, 0),
                    new(-1, -1, 0)
                },
                triangles = new[]
                {
                    0, 3, 1, 0, 2, 3
                },
                uv = new Vector2[]
                {
                    new(1, 1),
                    new(0, 1),
                    new(1, 0),
                    new(0, 0)
                }
            };


        private void Start()
        {
            Initialize();
        }
        private void Update()
        {
            CommitRender();
        }
    }
}