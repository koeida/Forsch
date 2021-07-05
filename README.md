# Forsch
Toy FORTH clone in C#.

Some example code pulled from the test runner:

    ( Basic Arithmetic )
    1 1 + 2 = ASSERT
    1.1 8.9 + 10.0 = ASSERT
    2 10 * 20 = ASSERT
    1.25 2.0 * 2.5 = ASSERT

    ( Exercising builtin words )
    1 2 SWAP 1 = ASSERT 2 = ASSERT
    1 2 DROP 1 = ASSERT
    true true = ASSERT
    foo foo = ASSERT
    1 Testing_dot_word_please_ignore_me . 1 = ASSERT
    Testing_survey_word_please_ignore_me SURVEY Testing_survey_word_please_ignore_me = ASSERT ( Make sure survey doesn't affect stack )

    ( Defining new words )
    : DOUBLE DUP + ;
    5 DOUBLE 10 = ASSERT
    : - -1 * + ; ( Being cute and defining a new operator )
    10 2 - 8 = ASSERT

    ( Defining new words that use other new words )
    : QUAD DOUBLE DOUBLE ;
    4 QUAD 16 = ASSERT

    ( Conditional Logic )
    : BISCUIT_TEST START_VAL 10 10 = IF BISCUITS THEN BISCUITS = ASSERT START_VAL = ASSERT ; 
    : BISCUIT 10 8 = IF BISCUITS THEN EMPTY? ASSERT ; 
    BISCUIT_TEST
    BISCUIT_TEST2

For a riveting demo, assuming you have python installed with Tk support, try piping the output of turtle.forsch to python:

    Forsch.exe < turtle.forsch | python

You should see a field of b e a u t i f u l daffodils grow before your eyes.

C# that interprets forth that outputs python that controls a turtle! It makes me happy.
