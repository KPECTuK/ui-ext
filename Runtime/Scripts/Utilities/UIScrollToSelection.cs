/// Credit zero3growlithe
/// sourced from: http://forum.unity3d.com/threads/scripts-useful-4-6-scripts-collection.264161/page-2#post-2011648

/*USAGE:
Simply place the script on the ScrollRect that contains the selectable children you will be scrolling 
*/

using System;
using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extensions
{
    [RequireComponent(typeof(ScrollRect))]
	[AddComponentMenu("UI/Extensions/UIScrollToSelection")]
	public class UIScrollToSelection : MonoBehaviour
	{
		#region MEMBERS

		[Header("[ Scroll settings ]")]
		[SerializeField]
		private float scrollSpeed = 50;
		[SerializeField, Tooltip("Scroll speed used when element to select is out of \"JumpOffsetThreshold\" range")]
		private float endOfListJumpScrollSpeed = 150;
		[SerializeField, Range(0, 1), Tooltip("If next element to scroll to is located over this screen percentage, use \"EndOfListJumpScrollSpeed\" to reach this element faster.")]
		private float jumpOffsetThreshold = 1;
		[SerializeField]
		private bool cancelScrollOnClick = true;

		[Header("[ Extended references ]")]
		[SerializeField, Tooltip("Scroll rect used to reach target element")]
		private ScrollRect targetScrollRect;

		// INTERNAL - MEMBERS ONLY
		private Vector3[] scrollRectCorners = new Vector3[4];
		private Vector3[] selectedElementCorners = new Vector3[4];

        #endregion

        #region PROPERTIES

        // REFERENCES
        public ScrollRect TargetScrollRect
		{
			get { return targetScrollRect; }
			set { targetScrollRect = value; }
		}

		// SETTINGS
		public float BaseScrollSpeed => scrollSpeed;
		public float EndOfListJumpScrollSpeed => endOfListJumpScrollSpeed;
		public float JumpOffsetThreshold=> jumpOffsetThreshold;

		// VARIABLES
		private RectTransform scrollRectTransform;
		private RectTransform contentTransform;

		private GameObject lastCheckedSelection;
		private RectTransform lastCheckedSelectionRect;
		private bool wasAutoScrollInterrupted;
		private bool isEndToEndJumping;

		#endregion

		#region FUNCTIONS

		protected void Awake()
		{
            if (!targetScrollRect)
            {
				targetScrollRect = GetComponent<ScrollRect>();
            }
            if (!targetScrollRect)
            {
				Debug.LogError("No ScrollRect attached to this component and no TargetScrollRect configured");
				gameObject.SetActive(false);
				return;
			}
			if (EventSystem.current == null)
			{
				Debug.LogError("[UIScrollToSelection] Unity UI EventSystem not found. It is required to check current selected object.");
				gameObject.SetActive(false);
				return;
			}
			scrollRectTransform = TargetScrollRect.GetComponent<RectTransform>();
			contentTransform = TargetScrollRect.content;
		}

		protected void LateUpdate()
		{
			UpdateProperties();
			UpdateScrollPosition();
		}

		protected void Reset()
		{
			TargetScrollRect = gameObject.GetComponentInParent<ScrollRect>();
		}

		private void UpdateProperties()
		{
			// update selection world corners
			if (lastCheckedSelectionRect != null)
			{
				lastCheckedSelectionRect.GetWorldCorners(selectedElementCorners);
			}

			// update references if selection changed
			GameObject selection = EventSystem.current.currentSelectedGameObject;

			if (selection == null || selection.activeSelf == false || selection == lastCheckedSelection ||
				selection.transform.IsChildOf(transform) == false)
			{
				return;
			}

			lastCheckedSelection = selection;
			lastCheckedSelectionRect = selection.GetComponent<RectTransform>();

			wasAutoScrollInterrupted = false;

			// scroll rect world corners
			scrollRectTransform.GetWorldCorners(scrollRectCorners);
		}

		private void UpdateScrollPosition()
		{
			// initial check if we can scroll at all
			if (lastCheckedSelection == null || wasAutoScrollInterrupted == true)
			{
				return;
			}

            // another check if we were not locked out by something else in scroll rect
            if (cancelScrollOnClick && Input.GetMouseButtonDown(0) == true)
            {
                wasAutoScrollInterrupted = true;

                return;
            }

			Vector2 scrollValue = Vector2.zero;

			scrollValue.x =
				(selectedElementCorners[0].x < scrollRectCorners[0].x ? selectedElementCorners[0].x - scrollRectCorners[0].x : 0) +
				(selectedElementCorners[2].x > scrollRectCorners[2].x ? selectedElementCorners[2].x - scrollRectCorners[2].x : 0);
			scrollValue.y =
				(selectedElementCorners[0].y < scrollRectCorners[0].y ? selectedElementCorners[0].y - scrollRectCorners[0].y : 0) +
				(selectedElementCorners[1].y > scrollRectCorners[1].y ? selectedElementCorners[1].y - scrollRectCorners[1].y : 0);

			if (scrollValue.x == 0 && scrollValue.y == 0)
			{
				isEndToEndJumping = false;
			}
			else if (Math.Abs(scrollValue.x) / Screen.width >= JumpOffsetThreshold || Math.Abs(scrollValue.y) / Screen.height >= JumpOffsetThreshold)
			{
				isEndToEndJumping = true;
			}

			// calculate scroll speeds
			float scrollSpeed = isEndToEndJumping ? EndOfListJumpScrollSpeed : BaseScrollSpeed;
			float horizontalSpeed = (Screen.width / Screen.dpi) * scrollSpeed;
			float verticalSpeed = (Screen.width / Screen.dpi) * scrollSpeed;

			// update target scroll rect
			Vector3 newPosition = contentTransform.localPosition;

			newPosition.x = Mathf.MoveTowards(newPosition.x, newPosition.x - scrollValue.x, horizontalSpeed * Time.unscaledDeltaTime);
			newPosition.y = Mathf.MoveTowards(newPosition.y, newPosition.y - scrollValue.y, verticalSpeed * Time.unscaledDeltaTime);

			var distance = Vector2.Distance(contentTransform.localPosition, newPosition);

			contentTransform.localPosition = newPosition;
		}

		#endregion
	}
}