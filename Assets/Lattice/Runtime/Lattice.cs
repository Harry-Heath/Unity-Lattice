using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lattice
{
	[ExecuteAlways]
	public class Lattice : MonoBehaviour
	{
		[SerializeField] private Vector3Int _resolution = new(2, 2, 2);
		[SerializeField] private List<LatticeHandle> _handles = new();

		private readonly List<Vector3> _offsets = new();

		/// <summary>
		/// The lattice's handles. These are created when the lattice is setup.
		/// </summary>
		public IReadOnlyList<LatticeHandle> Handles => _handles;

		/// <summary>
		/// The resolution of the lattice.<br/>
		/// To change this use <see cref="Setup(Vector3Int)"/>
		/// </summary>
		public Vector3Int Resolution => _resolution;

		/// <summary>
		/// The offsets to be used in deformation.<br/>
		/// These are automatically updated as part of <see cref="LateUpdate"/>.<br/>
		/// To manually set them use <see cref="SetHandleOffset(int, int, int, Vector3)"/>
		/// </summary>
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

		#region Public Methods

		/// <summary>
		/// Gets the handle by index. Index will be clamped to resolution.
		/// </summary>
		public LatticeHandle GetHandle(int x, int y, int z)
		{
			return _handles[GetIndex(x, y, z)];
		}

		/// <summary>
		/// Gets the current offset from the handle's base position.
		/// </summary>
		public Vector3 GetHandleOffset(int x, int y, int z)
		{
			return _handles[GetIndex(x, y, z)].offset;
		}

		/// <summary>
		/// Set the offset of a handle relative to it's base position.
		/// </summary>
		public void SetHandleOffset(int x, int y, int z, Vector3 offset)
		{
			_handles[GetIndex(x, y, z)].offset = offset;
		}

		/// <summary>
		/// Gets the position of a handle including current offset.
		/// </summary>
		public Vector3 GetHandlePosition(int x, int y, int z)
		{
			return _handles[GetIndex(x, y, z)].offset + GetBasePosition(x, y, z);
		}

		/// <summary>
		/// Set the position of a handle using a local transform position.
		/// </summary>
		public void SetHandlePosition(int x, int y, int z, Vector3 position)
		{
			_handles[GetIndex(x, y, z)].offset = position - GetBasePosition(x, y, z);
		}

		/// <summary>
		/// Gets the position of a handle before any offset.
		/// </summary>
		public Vector3 GetBasePosition(int x, int y, int z)
		{
			return new Vector3(
				x / (_resolution.x - 1f),
				y / (_resolution.y - 1f),
				z / (_resolution.z - 1f)
			);
		}

		/// <summary>
		/// Sets up the lattice for the desired resolution.
		/// </summary>
		/// <param name="resolution">The desired resolution</param>
		public void Setup(Vector3Int resolution)
		{
			// Delete existing handles
			LatticeHandle[] existingHandles = GetComponentsInChildren<LatticeHandle>();
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

			// Update resolution
			_resolution = resolution;

			// Create new handles
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

		/// <inheritdoc cref="Setup(Vector3Int)"/>
		public void Setup(int x, int y, int z) => Setup(new Vector3Int(x, y, z));

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the array index from a 3d handle index.
		/// </summary>
		private int GetIndex(int x, int y, int z) => x + _resolution.x * y + _resolution.x * _resolution.y * z;

		#endregion

		#region Unity Methods

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

		#endregion
	}
}