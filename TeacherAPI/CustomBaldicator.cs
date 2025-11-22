using MTM101BaldAPI.Components.Animation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TeacherAPI
{
	internal enum BaldicatorAnim 
	{
		Static,
		Animated
	}

	/// <summary>
	/// Not sure if it triggers issues with wide screens
	/// </summary>
	public class CustomBaldicator : MonoBehaviour
	{
		internal static List<CustomBaldicator> baldicators = new List<CustomBaldicator>();

        private CustomImageAnimator animator;

        /// <summary>
        /// Positions for the Baldicator, DO NOT CHANGE THE X AXIS AS IT GETS AUTOMATICALLY POSITIONED VIA <see cref="RearrangeBaldicators"/>.
        /// </summary>
        public Vector2 StartingPosition = new Vector2(0, -96), EndingPosition = new Vector2(0, 0);
		private IEnumerator animation;
        public float posspeed = 6f, framedelay = 0.75f;

        private Image image;
		private new RectTransform transform;

		internal void Awake()
		{
			image = GetComponent<Image>();
            transform = GetComponent<RectTransform>();
            animator = gameObject.AddComponent<CustomImageAnimator>();
			animator.image = image;
			baldicators.Add(this);
        }

		public static void RearrangeBaldicators()
		{
            bool baldiExists = TeacherManager.Instance.MainTeacherPrefab.Character == Character.Baldi || TeacherManager.Instance.assistingTeachersPrefabs.Exists(x => x.Character == Character.Baldi);
			float
				clampedPos = -96f / Mathf.CeilToInt((baldiExists ? 1f : 0f + baldicators.Count) / 3f),
				nextPos = baldiExists ? clampedPos : 0f;
			foreach (var baldicator in baldicators)
			{
				baldicator.StartingPosition = new Vector2(nextPos, baldicator.StartingPosition.y);
                baldicator.EndingPosition = new Vector2(nextPos, baldicator.EndingPosition.y);
				nextPos += clampedPos;
                baldicator.transform.anchoredPosition = baldicator.StartingPosition;
            }
		}

		private void OnDestroy() => baldicators.Remove(this);

        public void ActivateBaldicator(string animationToPlay)
		{
			if (animation != null) StopCoroutine(animation);
			animation = BaldicatorActivateAnimation(animationToPlay);
            StartCoroutine(animation);
		}

		public void AddAnimation(string key, SpriteAnimation sprites)
		{
			animator.AddAnimation(key, sprites);
            var staticsprite = new SpriteAnimation(new Sprite[] { sprites.frames.Last().value }, sprites.animationLength);
            animator.AddAnimation(key+"_Static", staticsprite);
        }
		public void SetHearingAnimation(SpriteAnimation sprites)
		{
			animator.AddAnimation("Hearing", sprites);
			var staticsprite = new SpriteAnimation(new Sprite[] { sprites.frames.Last().value }, sprites.animationLength);
            animator.AddAnimation("Hearing_Static", staticsprite);
            animator.SetDefaultAnimation("Hearing_Static", 0);
        }

        /// <summary>
        /// Create a custom baldicator for your teacher
        /// </summary>
        /// <returns>An instantiated <see cref="CustomBaldicator"/></returns>
        public static CustomBaldicator CreateBaldicator()
		{
			var hudManager = CoreGameManager.Instance.GetHud(0);
			var baldiclone = Instantiate(hudManager.gameObject.transform.Find("Baldi").gameObject, hudManager.transform, true);
			baldiclone.name = "Custom Baldicator";
			return baldiclone.AddComponent<CustomBaldicator>();
		}

		private IEnumerator BaldicatorActivateAnimation(string animationToPlay)
		{
            animator.SetDefaultAnimation("Hearing_Static", 1);
            animator.Play("Hearing", 1);
            for (float i = 0; i < 1; i += posspeed * Time.deltaTime)
			{
				transform.anchoredPosition = Vector3.Lerp(StartingPosition, EndingPosition, i);
				yield return null;
            }
			transform.anchoredPosition = EndingPosition;
            while (animator.name == "Hearing")
                yield return null;
            yield return new WaitForSeconds(framedelay);
            animator.SetDefaultAnimation(animationToPlay + "_Static", 1);
            animator.Play(animationToPlay, 1);
            while (animator.name == animationToPlay)
                yield return null;
            yield return new WaitForSeconds(framedelay);
            for (float i = 0; i < 1; i += posspeed * Time.deltaTime)
			{
                transform.anchoredPosition = Vector3.Lerp(EndingPosition, StartingPosition, i);
				yield return null;
            }
			transform.anchoredPosition = StartingPosition;
            yield break;
		}
	}
}
