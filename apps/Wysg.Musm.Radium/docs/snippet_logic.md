# After selection and insertion of snippet in the completion window, the snippet mode is started. (the key press to insert snippet is not counted as key press in the snippet mode.)

## every placeholder is highlighted and the caret is at the first placeholder.
## the caret cannot move out of the current placeholder until the placeholder is completed.
## on enter key the snippet mode is ended and the caret moves to the next line.
## on escape key the snippet mode is ended and the caret moves to the end of the inserted snippet.

### placeholder types:
1. free text placeholder
	- syntax: "${placeholdertext}"
	- the placeholder is replaced to input text
	- after inserting strings, the tab key result in completion of current placeholder.
	- if the snippet mode is ended before completion of current placeholder, the placeholder text is replaced to "[ ]".

2. mode 1: single choice placeholder
	- syntax: "${1^placeholdertext=a^choice1|b^choice2|3^choice3}" 
	- input of a single character immediately replaces the placeholder text to the corresponding choice text, and results in immediate completion of current placeholder.
	- the "1" at the front is indicator of "mode 1 snippet". the "placeholdertext" is placeholder text. when 'a' is input, the placeholder text is immediately replaced to "choice1".	
	- if the snippet mode is ended before completion of current placeholder, the placeholder text is replaced to the first choice text. in this example, if the snippet mode is ended before selection of choice, the placeholder text is replaced to "choice1".

3. mode 2: multiple choices placeholder
	- syntax: "${2^placeholdertext^option1^option2=a^choice1|b^choice2|3^choice3}" 
	- the placeholder is replaced to concat of multiple choices, with specific rules.
	- if the placeholder is "${2^pt^or=a^choice1|b^choice2|3^choice3}"
		- "2" at the front is indicator of "mode 2 snippet". 
		- the "pt" is the placeholder text. 
		- the options are in between the placeholder text and "=". In this example, there is an "or" option, which indicates the choices will be listed with "or".
			- if there is no "or" option, the default is "and".
			- other options include "bilateral" in which if there is "right sth" and "left sth", it will be transformed to "bilateral sth".
			- the options can be multiple.
		- if the "a" is input and the tab is pressed, the result will be "choice1". 
		- if the "ab3" or "b3a" is input and the tab is pressed, the result will be "choice1, choice2, or choice3".
	- unlike mode 1, the tab press is necessary to complete the snippet.
	- if the snippet mode is ended before completion of current placeholder, all choices are inserted. in this example, if the snippet mode is ended before completion of current placeholder, the placeholder text is replaced to "choice1, choice2, or choice3".

4. mode 3: single replace placeholder
	- syntax: ${3^placeholdertext=aa^choice1|bb^choice2|33^choice3}
	- the placeholder is replaced to the single text of choice input
	- the "3" at the front is indicator of "mode 3 snippet". the "placeholdertext" is placeholder text.
	- when 'aa' is input and tab is pressed, the placeholder text is replaced to "choice1".
	- unlike mode 1, the tab press is necessary to complete the snippet.
	- if the snippet mode is ended before completion of current placeholder, the placeholder text is replaced to the first choice text. in this example, if the snippet mode is ended before selection of choice, the placeholder text is replaced to "choice1". 


## completion of current placeholder result in remove of highlight and immediate move to the next placeholder.
## if there is no next placeholder, the snippet mode is ended and the caret moves to the end of the inserted snippet.