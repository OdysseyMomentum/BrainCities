import React, { Dispatch, SetStateAction, useState } from 'react';
import { Config } from '../types';

const engine = require('store/src/store-engine');
const storages = [require('store/storages/localStorage'), require('store/storages/cookieStorage')];
const plugins = [require('store/plugins/defaults'), require('store/plugins/expire')];
const store = engine.createStore(storages, plugins);

export interface GlobalContextType {
    config?: Config;
    changeConfig?: (config?: Config) => void;
}

export const GlobalContext = React.createContext<GlobalContextType>({});

const GlobalContextProvider = (props: { children: React.ReactChildren }) => {
    const savedConfig = store.get('config');

    const [config, setConfig] = useState<Config>(savedConfig || { smartLights: {} });

    const changeConfig = (newConfig?: Config) => {
        setConfig({
            ...config,
            ...newConfig,
        });
        store.set('config', {
            ...config,
            ...newConfig,
        });
    };

    const globalContext: GlobalContextType = {
        config,
        changeConfig,
    };

    return <GlobalContext.Provider value={globalContext}>{props.children}</GlobalContext.Provider>;
};

const wrapWithProvider = ({ element }: { element: React.ReactChildren }) => (
    <GlobalContextProvider>{element}</GlobalContextProvider>
);

export default wrapWithProvider;
