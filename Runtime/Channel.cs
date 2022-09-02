using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UltraCombos
{
	[ExecuteAlways]
	public class Channel : MonoBehaviour
	{
		public Slider slider;
		public Text numberText;
		public Button button;
		public Text valueText;

		[Range( 1, 512 )]
		public int channel = 1;
		[Range( 0, 255 )]
		public int value = 0;

		public UnityEvent onTrigger = new UnityEvent();

		private void Start()
		{
			slider.onValueChanged.AddListener( v =>
			 {
				 value = (int)v;
			 } );

			button.onClick.AddListener( () =>
			 {
				 onTrigger.Invoke();
			 } );
		}

		private void Update()
		{
			slider.value = value;

			numberText.text = $"{channel}";
			valueText.text = $"{value}";
		}
	}

}
