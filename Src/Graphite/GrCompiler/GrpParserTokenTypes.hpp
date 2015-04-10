#ifndef INC_GrpParserTokenTypes_hpp_
#define INC_GrpParserTokenTypes_hpp_

#include "antlr/config.hpp"
/*
 * ANTLR-generated file resulting from grammar c:\fw\src\graphite\grcompiler\grpparser.g
 *
 * Terence Parr, MageLang Institute
 * with John Lilley, Empathy Software
 * ANTLR Version 2.6.0; 1996-1999
 */

enum GrpParserTokenTypes {
	EOF_ = 1,
	NULL_TREE_LOOKAHEAD = 3,
	OP_EQ = 4,
	OP_PLUSEQUAL = 5,
	OP_LPAREN = 6,
	OP_RPAREN = 7,
	OP_SEMI = 8,
	LITERAL_environment = 9,
	LITERAL_endenvironment = 10,
	OP_LBRACE = 11,
	OP_RBRACE = 12,
	IDENT = 13,
	LITERAL_table = 14,
	LITERAL_endtable = 15,
	LITERAL_name = 16,
	LIT_INT = 17,
	OP_DOT = 18,
	OP_PLUS_EQUAL = 19,
	LIT_STRING = 20,
	OP_COMMA = 21,
	LITERAL_string = 22,
	LITERAL_glyph = 23,
	LITERAL_pseudo = 24,
	LIT_UHEX = 25,
	LITERAL_codepoint = 26,
	LITERAL_glyphid = 27,
	LITERAL_postscript = 28,
	LITERAL_unicode = 29,
	OP_DOTDOT = 30,
	LIT_CHAR = 31,
	LITERAL_feature = 32,
	LITERAL_language = 33,
	LITERAL_languages = 34,
	LITERAL_substitution = 35,
	LITERAL_pass = 36,
	LITERAL_endpass = 37,
	LITERAL_if = 38,
	LITERAL_else = 39,
	LITERAL_endif = 40,
	Zelseif = 41,
	LITERAL_elseif = 42,
	OP_GT = 43,
	OP_DIV = 44,
	OP_QUESTION = 45,
	OP_LBRACKET = 46,
	OP_RBRACKET = 47,
	OP_UNDER = 48,
	OP_AT = 49,
	OP_COLON = 50,
	OP_HASH = 51,
	OP_DOLLAR = 52,
	Qalias = 53,
	LITERAL_justification = 54,
	LITERAL_position = 55,
	LITERAL_positioning = 56,
	LITERAL_linebreak = 57,
	OP_CARET = 58,
	OP_MINUSEQUAL = 59,
	OP_DIVEQUAL = 60,
	OP_MULTEQUAL = 61,
	OP_OR = 62,
	OP_AND = 63,
	OP_EQUALEQUAL = 64,
	OP_NE = 65,
	OP_LT = 66,
	OP_LE = 67,
	OP_GE = 68,
	OP_PLUS = 69,
	OP_MINUS = 70,
	OP_MULT = 71,
	OP_NOT = 72,
	LITERAL_true = 73,
	LITERAL_false = 74,
	LITERAL_max = 75,
	LITERAL_min = 76,
	Zalias = 77,
	Zassocs = 78,
	Zattrs = 79,
	Zcluster = 80,
	Zcodepage = 81,
	Zconstraint = 82,
	Zcontext = 83,
	Zdirectives = 84,
	ZdotStruct = 85,
	Zfeatures = 86,
	Zfunction = 87,
	ZifStruct = 88,
	Zlhs = 89,
	Zlookup = 90,
	Zrhs = 91,
	Zrule = 92,
	ZruleItem = 93,
	Zselector = 94,
	Ztop = 95,
	ZuHex = 96,
	WS = 97,
	COMMENT_SL = 98,
	COMMENT_ML = 99,
	ESC = 100,
	ODIGIT = 101,
	DIGIT = 102,
	XDIGIT = 103,
	SQUOTE = 104,
	DQUOTE = 105,
	OP_LINEMARKER = 106,
	OP_BSLASH = 107,
	AT_IDENT = 108,
};
#endif /*INC_GrpParserTokenTypes_hpp_*/