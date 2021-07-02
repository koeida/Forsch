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

    ( Branch word successfully jumps over the correct number of words )
    50 False BRANCHF 2 10 20 20 = ASSERT 50 = ASSERT
