using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Heath.Lattice
{
	public class LatticeHandle : MonoBehaviour
	{
		[SerializeField, HideInInspector] private Lattice _lattice;
		[SerializeField, HideInInspector] private Vector3Int _coords;

		public Lattice Lattice
		{
			get => _lattice;
			internal set => _lattice = value;
		}

		public Vector3Int Coords
		{
			get => _coords;
			internal set => _coords = value;
		}

		public Vector3 Offset
		{
			get => transform.localPosition - _lattice.GetBasePosition(_coords.x, _coords.y, _coords.z);
			set => transform.localPosition = value + _lattice.GetBasePosition(_coords.x, _coords.y, _coords.z);
		}

		public Vector3 Position
		{
			get => transform.localPosition;
			set => transform.localPosition = value;
		}
	}
}