
import pandas as pd
import regex as re
import spacy

def to_lower(query_):  
    return str.lower(query_)

def remove_punctuation(query_): 
    return re.sub(r'[^\w\s]','', query_)