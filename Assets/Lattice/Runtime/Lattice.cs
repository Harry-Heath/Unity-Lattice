using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Heath.Lattice
{
	[ExecuteAlways]
	public class Lattice : MonoBehaviour
	{
		[SerializeField] internal Vector3Int _resolution = new(2, 2, 2);
		[SerializeField] internal List<LatticeHandle> _handles = new();
		private readonly List<Vector3> _offsets = new();

		public IReadOnlyList<LatticeHandle> Handles => _handles;
		public Vector3Int Resolution => _resolution;

		public List<Vector3> Offsets
		{
			get
			{
				if (_offsets.Count != _handles.Count)
				{
					_offsets.Clear();
					_offsets.AddRange(_handles.Select(h => h.offset));
				}
				
				return _offsets;
			}
		}

		public void Setup(Vector3Int resolution)
		{
			var existingHandles = GetComponentsInChildren<LatticeHandle>();
			for (int i = 0; i < existingHandles.Length; i++)
			{
				if (existingHandles[i] != null)
				{
					if (Application.isPlaying)
					{
						Destroy(existingHandles[i].gameObject);
					}
					else
					{
						DestroyImmediate(existingHandles[i].gameObject);
					}
				}
			}

			_resolution = resolution;
			_handles.Clear();

			for (int k = 0; k < _resolution.z; k++)
			{
				for (int j = 0; j < _resolution.y; j++)
				{
					for (int i = 0; i < _resolution.x; i++)
					{
						GameObject childObject = new($"Handle ({i}, {j}, {k})");
						childObject.transform.parent = transform;
						childObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
						childObject.hideFlags |= HideFlags.HideInHierarchy;

						LatticeHandle handle = childObject.AddComponent<LatticeHandle>();
						_handles.Add(handle);
					}
				}
			}
		}

		private Vector3 GetBasePosition(int x, int y, int z)
		{
			return new Vector3(
				x / (_resolution.x - 1f),
				y / (_resolution.y - 1f),
				z / (_resolution.z - 1f)
			);
		}

		public LatticeHandle GetHandle(int x, int y, int z)
		{
			return _handles[GetIndex(x, y, z)];
		}

		public Vector3 GetHandleOffset(int x, int y, int z)
		{
			return _handles[GetIndex(x, y, z)].offset;
		}

		public void SetHandleOffset(int x, int y, int z, Vector3 offset)
		{
			_handles[GetIndex(x, y, z)].offset = offset;
		}

		public Vector3 GetHandlePosition(int x, int y, int z)
		{
			return _handles[GetIndex(x, y, z)].offset + GetBasePosition(x, y, z);
		}

		public void SetHandlePosition(int x, int y, int z, Vector3 position)
		{
			_handles[GetIndex(x, y, z)].offset = position - GetBasePosition(x, y, z);
		}

		public void Setup(int x, int y, int z) => Setup(new Vector3Int(x, y, z));

		private int GetIndex(int x, int y, int z) => x + _resolution.x * y + _resolution.x * _resolution.y * z;

		private void LateUpdate()
		{
			if (Handles != null && Offsets != null)
			{
				for (int i = 0; i < _handles.Count; i++)
				{
					_offsets[i] = _handles[i].offset;
				}
			}
		}

		private void OnValidate()
		{
			_resolution = Vector3Int.Max(2 * Vector3Int.one, _resolution);
		}
	}
}