using UnityEngine;
using UnityEngine.Rendering;

namespace AllIn1SpriteShader
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(SpriteRenderer))]
	public class SpritePropertiesSync : MonoBehaviour
	{
		public enum CastShadowMode
		{
			[InspectorName("Off")] OFF,
			[InspectorName("One Sided")] ONE_SIDED,
			[InspectorName("Two Sided")] TWO_SIDED
		}

		private static int PROPID_SPRITE_FLIP = Shader.PropertyToID("_SpriteFlip");

		public SpriteRenderer spr;
		public bool isMaterialDrivenByAnimator;

		private Material mat;

		[SerializeField] private CastShadowMode shadowCastingMode = CastShadowMode.TWO_SIDED;

		void Start()
		{
			spr = GetComponent<SpriteRenderer>();
			mat = spr.sharedMaterial;
		}

		void LateUpdate()
		{
			if(mat != null && spr != null)
			{
				UpdateSpriteRenderer();
				UpdateMaterial();
			}
		}

		private void UpdateSpriteRenderer()
		{
			switch (shadowCastingMode)
			{
				case CastShadowMode.OFF:
					spr.shadowCastingMode = ShadowCastingMode.Off;
					break;
				case CastShadowMode.ONE_SIDED:
					spr.shadowCastingMode = ShadowCastingMode.On;
					break;
				case CastShadowMode.TWO_SIDED:
					spr.shadowCastingMode = ShadowCastingMode.TwoSided;
					break;
			}
		}

		private void UpdateMaterial()
		{
			float flipX = spr.flipX ? -1f : 1f;
			float flipY = spr.flipY ? -1f : 1f;

			Vector4 vecFlip = new Vector4(flipX, flipY, flipX, flipY);
			if (Application.isPlaying && isMaterialDrivenByAnimator)
			{
				vecFlip.x = 1.0f;
				vecFlip.y = 1.0f;
			}

			mat.SetVector(PROPID_SPRITE_FLIP, vecFlip);
		}
	}
}
