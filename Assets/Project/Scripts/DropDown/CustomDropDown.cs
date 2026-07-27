using UnityEngine;

using UnityEngine.UI;

using TMPro;

using UnityEngine.Events;






    public class CustomDropDown : MonoBehaviour

    {

        [Header("Main")]

        public Button headerButton;

        public TMP_Text headerText;

    

        [Header("Dropdown")]

        public GameObject optionsPanel;

        public Button[] optionButtons;

        public TMP_Text[] optionTexts;

    

        [Header("Icons")]

        public GameObject correctIcon;

        public GameObject wrongIcon;

    

        [Header("Settings")]

        [Tooltip("Index of correct option (0-3)")]

        public int correctOptionIndex;

    

        [Header("Events")]

        public UnityEvent onCorrectSelected;

        public UnityEvent onWrongSelected;

    

        private bool isOpen = false;

        private bool isCorrect = false;

    

        public bool IsCorrect => isCorrect;

    

        void Start()

        {

            optionsPanel.SetActive(false);

            correctIcon.SetActive(false);

            wrongIcon.SetActive(false);

    

            headerButton.onClick.AddListener(ToggleDropdown);

    

            for (int i = 0; i < optionButtons.Length; i++)

            {

                int index = i;

                optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));

            }

        }

    

        void ToggleDropdown()

        {

            isOpen = !isOpen;

            optionsPanel.SetActive(isOpen);

        }

    

        void OnOptionSelected(int index)

        {

            headerText.text = optionTexts[index].text;

    

            optionsPanel.SetActive(false);

            isOpen = false;

    

            if (index == correctOptionIndex)

            {

                isCorrect = true;

                correctIcon.SetActive(true);

                wrongIcon.SetActive(false);

    

                onCorrectSelected?.Invoke();

            }

            else

            {

                isCorrect = false;

                wrongIcon.SetActive(true);

                correctIcon.SetActive(false);

    

                onWrongSelected?.Invoke();

            }

        }

    }
    
