using MTM101BaldAPI;
using MTM101BaldAPI.Components;
using System.Collections;
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
		private CustomImageAnimator animator;

		private Vector2 StartingPosition = new Vector2(0, -96);
		private Vector2 EndingPosition = new Vector2(0, 0);
		private IEnumerator animation;
		[SerializeField] private float posspeed = 6f;
        [SerializeField] private float framedelay = 0.75f;

        private Image image;
		private new RectTransform transform;

		internal void Awake()
		{
			image = GetComponent<Image>();
            transform = GetComponent<RectTransform>();
            animator = gameObject.AddComponent<CustomImageAnimator>();
			animator.affectedObject = image;
		}

		public void ActivateBaldicator(string animationToPlay)
		{
			if (animation != null) StopCoroutine(animation);
			animation = BaldicatorActivateAnimation(animationToPlay);
            StartCoroutine(animation);
		}

		public void AddAnimation(string key, CustomAnimation<Sprite> sprites)
		{
			animator.animations.Add(key, sprites);
            var staticsprite = new CustomAnimation<Sprite>(new Sprite[] { sprites.frames.Last().value }, sprites.animationLength);
            animator.animations.Add(key+"_Static", staticsprite);
        }
		public void SetHearingAnimation(CustomAnimation<Sprite> sprites)
		{
			animator.animations.Add("Hearing", sprites);
			var staticsprite = new CustomAnimation<Sprite>(new Sprite[] { sprites.frames.Last().value }, sprites.animationLength);
            animator.animations.Add("Hearing_Static", staticsprite);
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
            while (animator.currentAnimationName == "Hearing")
                yield return null;
            yield return new WaitForSeconds(framedelay);
            animator.SetDefaultAnimation(animationToPlay + "_Static", 1);
            animator.Play(animationToPlay, 1);
            while (animator.currentAnimationName == animationToPlay)
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
