import React, { useContext, useEffect, useState } from 'react';
import { GlobalContext } from '../context/GlobalContextProvider';
import { navigate } from 'gatsby';
import { City, Config, Light } from '../components';
import Draggable from 'react-draggable';
import { SMART_LIGHT_IDS } from '../types';

const GetStartedPage = () => {
    return (
        <>
            <Config />
            <Light id={SMART_LIGHT_IDS.SL1} />
            <Light id={SMART_LIGHT_IDS.SL2} />
            <Light id={SMART_LIGHT_IDS.SL3} />
            <City />
        </>
    );
};

export default GetStartedPage;
