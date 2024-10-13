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

		private Vector3 StartingPosition = new Vector3(320, -276, 0);
		private Vector3 EndingPosition = new Vector3(320, -180, 0);
		private IEnumerator animation;
		[SerializeField] private float posspeed = 6f;
        [SerializeField] private float framedelay = 0.75f;

        private Image image;

		internal void Awake()
		{
			image = GetComponent<Image>();
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

		public static CustomBaldicator CreateBaldicator()
		{
			var hudManager = Singleton<CoreGameManager>.Instance.GetHud(0);
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
				transform.localPosition = Vector3.Lerp(StartingPosition, EndingPosition, i);
				yield return null;
            }
			transform.localPosition = EndingPosition;
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
                transform.localPosition = Vector3.Lerp(EndingPosition, StartingPosition, i);
				yield return null;
            }
			transform.localPosition = StartingPosition;
            yield break;
		}
	}
}
