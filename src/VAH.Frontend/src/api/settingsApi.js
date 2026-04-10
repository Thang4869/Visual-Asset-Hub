import apiClient from './client';

export const getSettings = () => {
    return apiClient.get('/settings').then(res => res.data);
};

export const updateSettings = (data) => {
    return apiClient.patch('/settings', data).then(res => res.data);
};
