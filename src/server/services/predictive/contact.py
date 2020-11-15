import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sb
from sklearn.linear_model import LinearRegression
import pickle
import os

model = pickle.load(open("./models/finalized_model_rul.sav", 'rb')) #linear regression model which was trained on historical data

threshold = 96

def get_rul(model = model, s_recordings = []): # s_recordings is a list of the recordings of 14 sensors
    X = pd.DataFrame(s_recordings).transpose()
    return model.predict(X)

def contact_stakeholder(rul_):
    if rul_ <= threshold:
        return 1 # contact
    else:
        return 0 # do not contact
