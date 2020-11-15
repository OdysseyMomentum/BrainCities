import pandas as pd
import numpy as np
import pickle
import preprocessing as pr
import spacy
from sentence_transformers import SentenceTransformer
import scipy.spatial.distance
import statistics

# this is a very small simple sample data - with the collection of technical reports and NLP more complex dataset can de designed
data = pd.read_excel("./data/sample_status_failure_reasons.xlsx") 

model = modelDesc = SentenceTransformer("xlm-r-bert-base-nli-stsb-mean-tokens")

def compute_similarity(right_embedded, left_embedded): 
  # function to compute similarity between two lists and saves the scores in a dictionary
  
  distances = scipy.spatial.distance.cdist([right_embedded], left_embedded, "cosine")[0]
  scores = [abs(1-x) for x in distances] # absolute in order to have the scores between 0 and 1

  return scores

def get_defaults(query = {"controller_status": "", "lumen": "", "voltage": "", "driver_current": ""},
                 number_top_matches: int = 2,
                 ds = data, 
                 ds_c_status_embeddings: list = [], 
                 ds_lm_embeddings: list = [],
                 ds_v_embeddings: list = [],
                 ds_dcurr_embeddings: list = [],
                 query_embeddings: dict = {},  
                 model = model, 
                 preprocess_ds: bool = False): 

      
    for key in query:

        if len(query[key]):
            query[key] = pr.to_lower(query[key])
            query[key] = pr.remove_punctuation(query[key])


    if preprocess_ds: 
      ds = ds.fillna("")
      ds["controller_status"] = ds["controller_status"].apply(lambda x: pr.to_lower(x))
      ds["controller_status"] = ds["controller_status"].apply(lambda x: pr.remove_punctuation(x))
     
      ds["lumen"] = ds["lumen"].apply(lambda x: pr.to_lower(x))
      ds["lumen"] = ds["lumen"].apply(lambda x: pr.remove_punctuation(x))
      
      ds["voltage"] = ds["voltage"].apply(lambda x: pr.to_lower(x))
      ds["voltage"] = ds["voltage"].apply(lambda x: pr.remove_punctuation(x))
      
      ds["driver_current"] = ds["driver_current"].apply(lambda x: pr.to_lower(x))
      ds["driver_current"] = ds["driver_current"].apply(lambda x: pr.remove_punctuation(x))
    

    if not ds_c_status_embeddings:
       ds_c = ds["controller_status"].tolist()
       ds_c_status_embeddings = [model.encode(c) for c in ds_c]
    
    if not ds_lm_embeddings:
       ds_lm = ds["lumen"].tolist()
       ds_lm_embeddings = [model.encode(lm) for lm in ds_lm]
       
    if not ds_v_embeddings:
        ds_v = ds["voltage"].tolist()
        ds_v_embeddings = [model.encode(v) for v in ds_v]
    
    if not ds_dcurr_embeddings:
        ds_dcurr = ds["driver_current"].tolist()
        ds_dcurr_embeddings = [model.encode(curr) for curr in ds_dcurr]


    scores = []

    query_embeddings = {}
    if len(query["controller_status"]):
      query_embeddings["controller_status"] = model.encode(query["controller_status"])
      contr_scores = compute_similarity(query_embeddings["controller_status"], ds_c_status_embeddings)
      scores.append(contr_scores)

    if len(query["lumen"]):
      query_embeddings["lumen"] = model.encode(query["lumen"])
      lm_scores = compute_similarity(query_embeddings["lumen"], ds_lm_embeddings)
      scores.append(lm_scores)

    if len(query["voltage"]):
      query_embeddings["voltage"] = model.encode(query["voltage"])
      v_scores = compute_similarity(query_embeddings["voltage"], ds_v_embeddings)
      scores.append(v_scores)

    if len(query["driver_current"]):
      query_embeddings["driver_current"] = model.encode(query["driver_current"])
      v_scores = compute_similarity(query_embeddings["driver_current"], ds_dcurr_embeddings)
      scores.append(v_scores)

    mean_score = np.mean(scores, axis=0).tolist()
    
    top_indices = [np.array(mean_score).argsort()[-number_top_matches:][::-1]]
    
    #sorted_scores = sorted(mean_score, reverse=True)
    #top_scores = sorted_scores[:number_top_matches]

    similar_cases = ds.iloc[top_indices[0]]
    
    result_df = pd.DataFrame()
    result_df["reason"] = similar_cases["reason"]

    return result_df