import { notification } from 'antd';

export const showNotification = (
    type?: 'success' | 'info' | 'warning' | 'error',
    title?: string,
    description?: string,
) => {
    notification[type]({
        message: title,
        description: description,
        placement: 'topLeft',
    });
};
